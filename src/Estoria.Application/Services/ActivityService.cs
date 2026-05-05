using Estoria.Application.Common;
using Estoria.Application.DTOs.CRM.Activities;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

public class ActivityService
{
    private readonly IAppDbContext _db;
    private readonly AuditService _audit;
    private readonly AuthorizationGuard _authz;

    public ActivityService(IAppDbContext db, AuditService audit, AuthorizationGuard authz)
    {
        _db    = db;
        _audit = audit;
        _authz = authz;
    }

    /// <summary>
    /// Filter-rich list. The new /admin/activities page passes type/user/date
    /// filters plus a free-text search across Title and Body; the legacy
    /// by-deal / by-contact callers still hit the same endpoint with the
    /// scoped Guids and ignore everything else.
    /// </summary>
    public async Task<PagedResult<ActivityDto>> GetListAsync(
        Guid? dealId             = null,
        Guid? contactId          = null,
        Guid? propertyId         = null,
        Guid? userId             = null,
        ActivityType? type       = null,
        DateTime? occurredAfter  = null,
        DateTime? occurredBefore = null,
        string? search           = null,
        int page                 = 1,
        int pageSize             = 50,
        CancellationToken ct     = default)
    {
        IQueryable<Activity> q = _db.Activities.AsNoTracking().Include(a => a.User);

        if (dealId.HasValue)        q = q.Where(a => a.DealId == dealId.Value);
        if (contactId.HasValue)     q = q.Where(a => a.ContactId == contactId.Value);
        if (propertyId.HasValue)    q = q.Where(a => a.PropertyId == propertyId.Value);
        if (userId.HasValue)        q = q.Where(a => a.UserId == userId.Value);
        if (type.HasValue)          q = q.Where(a => a.Type == type.Value);
        if (occurredAfter.HasValue) q = q.Where(a => a.OccurredAt >= occurredAfter.Value);
        if (occurredBefore.HasValue) q = q.Where(a => a.OccurredAt <= occurredBefore.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(a =>
                a.Title.ToLower().Contains(term) ||
                (a.Body != null && a.Body.ToLower().Contains(term)));
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ActivityDto>
        {
            Items      = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize,
        };
    }

    public async Task<ActivityDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var a = await _db.Activities
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return a is null ? null : ToDto(a);
    }

    public async Task<Guid> CreateAsync(ActivityWriteDto dto, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var activity = new Activity
        {
            DealId          = dto.DealId,
            ContactId       = dto.ContactId,
            PropertyId      = dto.PropertyId,
            UserId          = _authz.CurrentUserId,
            Type            = dto.Type,
            Title           = dto.Title.Trim(),
            Body            = dto.Body,
            OccurredAt      = dto.OccurredAt ?? DateTime.UtcNow,
            DurationMinutes = dto.DurationMinutes,
            Outcome         = dto.Outcome,
        };

        _db.Activities.Add(activity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Activity.Create",
            entityType: nameof(Activity),
            entityId: activity.Id,
            details: new { Type = activity.Type.ToString(), activity.Title, activity.DealId, activity.ContactId },
            ct: ct);

        return activity.Id;
    }

    public async Task UpdateAsync(Guid id, ActivityWriteDto dto, CancellationToken ct = default)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException($"Activity {id} not found.");

        // Activities are owned by their author. Admin can override.
        _authz.RequireOwnershipOrAdmin(activity.UserId);

        activity.DealId          = dto.DealId;
        activity.ContactId       = dto.ContactId;
        activity.PropertyId      = dto.PropertyId;
        activity.Type            = dto.Type;
        activity.Title           = dto.Title.Trim();
        activity.Body            = dto.Body;
        activity.OccurredAt      = dto.OccurredAt ?? activity.OccurredAt;
        activity.DurationMinutes = dto.DurationMinutes;
        activity.Outcome         = dto.Outcome;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Activity.Update",
            entityType: nameof(Activity),
            entityId: activity.Id,
            details: new { Type = activity.Type.ToString(), activity.Title },
            ct: ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException($"Activity {id} not found.");

        _authz.RequireOwnershipOrAdmin(activity.UserId);

        _db.Activities.Remove(activity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Activity.Delete",
            entityType: nameof(Activity),
            entityId: activity.Id,
            details: new { Type = activity.Type.ToString(), activity.Title },
            ct: ct);
    }

    private static ActivityDto ToDto(Activity a) => new()
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
    };
}
