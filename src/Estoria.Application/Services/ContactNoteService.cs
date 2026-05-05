using Estoria.Application.Common;
using Estoria.Application.DTOs.CRM.Notes;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

public class ContactNoteService
{
    private readonly IAppDbContext _db;
    private readonly AuditService _audit;
    private readonly AuthorizationGuard _authz;

    public ContactNoteService(IAppDbContext db, AuditService audit, AuthorizationGuard authz)
    {
        _db    = db;
        _audit = audit;
        _authz = authz;
    }

    public async Task<List<ContactNoteDto>> GetForContactAsync(Guid contactId, CancellationToken ct = default)
    {
        var rows = await _db.ContactNotes
            .AsNoTracking()
            .Include(n => n.User)
            .Where(n => n.ContactId == contactId)
            // Pinned notes first, then by recency
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(ToDto).ToList();
    }

    public async Task<Guid> CreateAsync(ContactNoteWriteDto dto, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var note = new ContactNote
        {
            ContactId = dto.ContactId,
            UserId    = _authz.CurrentUserId,
            Body      = dto.Body,
            IsPinned  = dto.IsPinned,
        };
        _db.ContactNotes.Add(note);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "ContactNote.Create",
            entityType: nameof(ContactNote),
            entityId: note.Id,
            details: new { note.ContactId, note.IsPinned },
            ct: ct);

        return note.Id;
    }

    public async Task UpdateAsync(Guid id, ContactNoteWriteDto dto, CancellationToken ct = default)
    {
        var note = await _db.ContactNotes.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"ContactNote {id} not found.");

        // Notes are owned by their author. Admin can override.
        _authz.RequireOwnershipOrAdmin(note.UserId);

        note.Body     = dto.Body;
        note.IsPinned = dto.IsPinned;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "ContactNote.Update",
            entityType: nameof(ContactNote),
            entityId: note.Id,
            details: new { note.IsPinned },
            ct: ct);
    }

    public async Task SetPinnedAsync(Guid id, bool isPinned, CancellationToken ct = default)
    {
        var note = await _db.ContactNotes.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"ContactNote {id} not found.");

        _authz.RequireOwnershipOrAdmin(note.UserId);

        if (note.IsPinned == isPinned) return;
        note.IsPinned = isPinned;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            isPinned ? "ContactNote.Pin" : "ContactNote.Unpin",
            entityType: nameof(ContactNote),
            entityId: note.Id,
            details: new { note.ContactId },
            ct: ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var note = await _db.ContactNotes.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"ContactNote {id} not found.");

        _authz.RequireOwnershipOrAdmin(note.UserId);

        _db.ContactNotes.Remove(note);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "ContactNote.Delete",
            entityType: nameof(ContactNote),
            entityId: note.Id,
            details: new { note.ContactId },
            ct: ct);
    }

    private static ContactNoteDto ToDto(ContactNote n) => new()
    {
        Id        = n.Id,
        ContactId = n.ContactId,
        UserId    = n.UserId,
        UserName  = n.User.FullName,
        Body      = n.Body,
        IsPinned  = n.IsPinned,
        CreatedAt = n.CreatedAt,
        UpdatedAt = n.UpdatedAt,
    };
}
