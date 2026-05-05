using Estoria.Application.Common;
using Estoria.Application.DTOs.Contact;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Estoria.Application.Services;

public class ContactMessageService
{
    private readonly IAppDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<ContactMessageService> _logger;
    private readonly AuditService _audit;

    public ContactMessageService(
        IAppDbContext db,
        IEmailService email,
        ILogger<ContactMessageService> logger,
        AuditService audit)
    {
        _db     = db;
        _email  = email;
        _logger = logger;
        _audit  = audit;
    }

    public async Task<Guid> SubmitAsync(
        CreateContactDto dto, CancellationToken ct = default)
    {
        var message = new ContactMessage
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Subject = dto.Subject,
            Message = dto.Message,
            PropertyId = dto.PropertyId
        };

        _db.ContactMessages.Add(message);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Contact.Create",
            entityType: nameof(ContactMessage),
            entityId: message.Id,
            details: new { message.Name, message.Email, message.PropertyId },
            ct: ct);

        _ = Task.Run(async () =>
        {
            try
            {
                await _email.SendContactNotificationAsync(
                    dto.Name, dto.Email, dto.Message, dto.Phone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact notification email for {Email}", dto.Email);
            }
        });

        return message.Id;
    }

    // -------------------------------------------------------------------------
    // Admin
    // -------------------------------------------------------------------------

    public async Task<PagedResult<ContactMessageDto>> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ContactMessages
            .AsNoTracking()
            .Include(cm => cm.Property)
                .ThenInclude(p => p!.Translations)
            .OrderByDescending(cm => cm.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ContactMessageDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task UpdateStatusAsync(
        Guid id, ContactStatus status, CancellationToken ct = default)
    {
        var message = await _db.ContactMessages.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"ContactMessage {id} not found.");

        message.Status = status;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Contact.Update",
            entityType: nameof(ContactMessage),
            entityId: message.Id,
            details: new { Status = status.ToString() },
            ct: ct);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static ContactMessageDto ToDto(ContactMessage cm)
    {
        var propertyTitle = cm.Property?.Translations
            .FirstOrDefault(t => t.Language == Language.En)?.Title
            ?? cm.Property?.Translations.FirstOrDefault()?.Title;

        return new ContactMessageDto
        {
            Id = cm.Id,
            Name = cm.Name,
            Email = cm.Email,
            Phone = cm.Phone,
            Subject = cm.Subject,
            Message = cm.Message,
            PropertyId = cm.PropertyId,
            PropertyTitle = propertyTitle,
            Status = cm.Status,
            CreatedAt = cm.CreatedAt
        };
    }
}
