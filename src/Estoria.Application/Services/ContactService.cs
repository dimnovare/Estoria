using Estoria.Application.Common;
using Estoria.Application.DTOs.CRM.Contacts;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

/// <summary>
/// CRM Contact (= person in the pipeline). Distinct from ContactMessageService
/// which handles the public website's contact-form submissions.
/// </summary>
public class ContactService
{
    private readonly IAppDbContext _db;
    private readonly AuditService _audit;
    private readonly AuthorizationGuard _authz;

    public ContactService(IAppDbContext db, AuditService audit, AuthorizationGuard authz)
    {
        _db    = db;
        _audit = audit;
        _authz = authz;
    }

    public async Task<PagedResult<ContactListDto>> GetListAsync(
        string? search,
        string? tag,
        ContactSource? source,
        Guid? agentId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        IQueryable<Contact> q = _db.Contacts.AsNoTracking().Include(c => c.AssignedAgent);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Case-insensitive partial match — ToLower().Contains() translates
            // to LOWER(...) LIKE '%term%' which is equivalent to ILIKE on
            // Postgres, without pulling Npgsql into the Application layer.
            var term = search.Trim().ToLower();
            q = q.Where(c =>
                c.FullName.ToLower().Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                (c.Phone != null && c.Phone.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(tag))
            q = q.Where(c => c.Tags.Contains(tag));

        if (source.HasValue)
            q = q.Where(c => c.Source == source.Value);

        if (agentId.HasValue)
            q = q.Where(c => c.AssignedAgentId == agentId.Value);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ContactListDto>
        {
            Items      = items.Select(ToListDto).ToList(),
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize,
        };
    }

    public async Task<ContactDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _db.Contacts
            .AsNoTracking()
            .Include(x => x.AssignedAgent)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return c is null ? null : ToDetailDto(c);
    }

    public async Task<List<DTOs.CRM.Deals.DealListDto>> GetDealsForContactAsync(
        Guid contactId, CancellationToken ct = default)
    {
        var deals = await _db.Deals
            .AsNoTracking()
            .Include(d => d.PrimaryContact)
            .Include(d => d.AssignedAgent)
            .Where(d => d.PrimaryContactId == contactId
                     || d.Participants.Any(p => p.ContactId == contactId))
            .OrderByDescending(d => d.StageChangedAt)
            .ToListAsync(ct);

        return deals.Select(d => new DTOs.CRM.Deals.DealListDto
        {
            Id                 = d.Id,
            Title              = d.Title,
            Stage              = d.Stage,
            DealType           = d.DealType,
            Side               = d.Side,
            PropertyId         = d.PropertyId,
            PrimaryContactId   = d.PrimaryContactId,
            PrimaryContactName = d.PrimaryContact.FullName,
            AssignedAgentId    = d.AssignedAgentId,
            AssignedAgentName  = d.AssignedAgent.FullName,
            ExpectedCloseDate  = d.ExpectedCloseDate,
            ExpectedValue      = d.ExpectedValue,
            ActualValue        = d.ActualValue,
            Currency           = d.Currency,
            StageChangedAt     = d.StageChangedAt,
            CreatedAt          = d.CreatedAt,
        }).ToList();
    }

    public async Task<List<DTOs.CRM.Activities.ActivityDto>> GetActivitiesForContactAsync(
        Guid contactId, CancellationToken ct = default)
    {
        var activities = await _db.Activities
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.ContactId == contactId)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(ct);

        return activities.Select(a => new DTOs.CRM.Activities.ActivityDto
        {
            Id              = a.Id,
            DealId          = a.DealId,
            ContactId       = a.ContactId,
            PropertyId      = a.PropertyId,
            UserId          = a.UserId,
            UserName        = a.User.FullName,
            Type            = a.Type,
            Title           = a.Title,
            Body            = a.Body,
            OccurredAt      = a.OccurredAt,
            DurationMinutes = a.DurationMinutes,
            Outcome         = a.Outcome,
            CreatedAt       = a.CreatedAt,
        }).ToList();
    }

    /// <summary>GDPR-style export: full contact + deals + activities + notes as a JSON-friendly object.</summary>
    public async Task<object> ExportAsync(Guid id, CancellationToken ct = default)
    {
        var contact = await _db.Contacts
            .AsNoTracking()
            .Include(c => c.AssignedAgent)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException($"Contact {id} not found.");

        var deals       = await GetDealsForContactAsync(id, ct);
        var activities  = await GetActivitiesForContactAsync(id, ct);

        var notes = await _db.ContactNotes
            .AsNoTracking()
            .Include(n => n.User)
            .Where(n => n.ContactId == id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.Id,
                n.Body,
                n.IsPinned,
                AuthorEmail = n.User.Email,
                n.CreatedAt,
            })
            .ToListAsync(ct);

        return new
        {
            ExportedAt = DateTime.UtcNow,
            Contact    = ToDetailDto(contact),
            Deals      = deals,
            Activities = activities,
            Notes      = notes,
        };
    }

    public async Task<Guid> CreateAsync(ContactWriteDto dto, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var contact = new Contact();
        ApplyWriteDto(contact, dto);
        _db.Contacts.Add(contact);

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Contact.Create",
            entityType: nameof(Contact),
            entityId: contact.Id,
            details: new { contact.FullName, contact.Email, contact.AssignedAgentId },
            ct: ct);

        return contact.Id;
    }

    public async Task UpdateAsync(Guid id, ContactWriteDto dto, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var contact = await _db.Contacts.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException($"Contact {id} not found.");

        // Agents can only edit contacts they own. Admin always allowed.
        if (contact.AssignedAgentId is { } owner)
            _authz.RequireOwnershipOrAdmin(owner);

        ApplyWriteDto(contact, dto);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Contact.Update",
            entityType: nameof(Contact),
            entityId: contact.Id,
            details: new { contact.FullName, contact.Email },
            ct: ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var contact = await _db.Contacts.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException($"Contact {id} not found.");

        if (contact.AssignedAgentId is { } owner)
            _authz.RequireOwnershipOrAdmin(owner);

        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Contact.Delete",
            entityType: nameof(Contact),
            entityId: contact.Id,
            details: new { contact.FullName, contact.Email },
            ct: ct);
    }

    private void ApplyWriteDto(Contact target, ContactWriteDto dto)
    {
        target.FullName          = dto.FullName.Trim();
        target.Email             = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant();
        target.Phone             = dto.Phone;
        target.SecondaryPhone    = dto.SecondaryPhone;
        target.PreferredLanguage = dto.PreferredLanguage;
        target.DateOfBirth       = dto.DateOfBirth;
        target.Address           = dto.Address;
        target.Company           = dto.Company;
        target.Position          = dto.Position;
        target.Source            = dto.Source;
        target.SourceDetail      = dto.SourceDetail;
        target.IsBuyer           = dto.IsBuyer;
        target.IsSeller          = dto.IsSeller;
        target.IsPartner         = dto.IsPartner;
        target.IsTenant          = dto.IsTenant;
        target.IsLandlord        = dto.IsLandlord;
        target.AssignedAgentId   = dto.AssignedAgentId;
        target.Notes             = dto.Notes;
        target.Tags              = dto.Tags?.ToList() ?? [];

        // Stamp consent timestamp the first time it flips on; clear on revoke.
        if (dto.ConsentMarketing != target.ConsentMarketing)
        {
            target.ConsentMarketing   = dto.ConsentMarketing;
            target.ConsentMarketingAt = dto.ConsentMarketing ? DateTime.UtcNow : null;
        }
    }

    private static ContactListDto ToListDto(Contact c) => new()
    {
        Id                = c.Id,
        FullName          = c.FullName,
        Email             = c.Email,
        Phone             = c.Phone,
        Source            = c.Source,
        AssignedAgentId   = c.AssignedAgentId,
        AssignedAgentName = c.AssignedAgent?.FullName,
        IsBuyer           = c.IsBuyer,
        IsSeller          = c.IsSeller,
        IsTenant          = c.IsTenant,
        IsLandlord        = c.IsLandlord,
        IsPartner         = c.IsPartner,
        Tags              = [.. c.Tags],
        CreatedAt         = c.CreatedAt,
    };

    private static ContactDetailDto ToDetailDto(Contact c) => new()
    {
        Id                 = c.Id,
        FullName           = c.FullName,
        Email              = c.Email,
        Phone              = c.Phone,
        SecondaryPhone     = c.SecondaryPhone,
        PreferredLanguage  = c.PreferredLanguage,
        DateOfBirth        = c.DateOfBirth,
        Address            = c.Address,
        Company            = c.Company,
        Position           = c.Position,
        Source             = c.Source,
        SourceDetail       = c.SourceDetail,
        IsBuyer            = c.IsBuyer,
        IsSeller           = c.IsSeller,
        IsPartner          = c.IsPartner,
        IsTenant           = c.IsTenant,
        IsLandlord         = c.IsLandlord,
        AssignedAgentId    = c.AssignedAgentId,
        AssignedAgentName  = c.AssignedAgent?.FullName,
        ConsentMarketing   = c.ConsentMarketing,
        ConsentMarketingAt = c.ConsentMarketingAt,
        Notes              = c.Notes,
        Tags               = [.. c.Tags],
        CreatedAt          = c.CreatedAt,
        UpdatedAt          = c.UpdatedAt,
    };
}

