using Estoria.Application.Common;
using Estoria.Application.DTOs.Newsletter;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Estoria.Application.Services;

public class NewsletterService
{
    private readonly IAppDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<NewsletterService> _logger;
    private readonly AuditService _audit;

    public NewsletterService(
        IAppDbContext db,
        IEmailService email,
        ILogger<NewsletterService> logger,
        AuditService audit)
    {
        _db     = db;
        _email  = email;
        _logger = logger;
        _audit  = audit;
    }

    public async Task SubscribeAsync(
        SubscribeDto dto, CancellationToken ct = default)
    {
        var lang = dto.Language ?? Language.En;

        var existing = await _db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == dto.Email, ct);

        if (existing is not null)
        {
            existing.IsActive = true;
            existing.Language = lang;
        }
        else
        {
            _db.NewsletterSubscribers.Add(new NewsletterSubscriber
            {
                Email        = dto.Email,
                Language     = lang,
                IsActive     = true,
                SubscribedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Newsletter.Subscribe",
            entityType: nameof(NewsletterSubscriber),
            details: new { dto.Email, Language = lang.ToString() },
            ct: ct);

        _ = Task.Run(async () =>
        {
            try
            {
                await _email.SendNewsletterWelcomeAsync(dto.Email, lang);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send newsletter welcome email to {Email}", dto.Email);
            }
        });
    }

    // -------------------------------------------------------------------------
    // Admin
    // -------------------------------------------------------------------------

    public async Task<PagedResult<SubscriberDto>> GetSubscribersAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.NewsletterSubscribers
            .AsNoTracking()
            .OrderByDescending(s => s.SubscribedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<SubscriberDto>
        {
            Items = items.Select(s => new SubscriberDto
            {
                Id = s.Id,
                Email = s.Email,
                Language = s.Language,
                IsActive = s.IsActive,
                SubscribedAt = s.SubscribedAt
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task UnsubscribeAsync(Guid id, CancellationToken ct = default)
    {
        var subscriber = await _db.NewsletterSubscribers.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Subscriber {id} not found.");

        subscriber.IsActive = false;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Newsletter.Unsubscribe",
            entityType: nameof(NewsletterSubscriber),
            entityId: subscriber.Id,
            details: new { subscriber.Email },
            ct: ct);
    }
}
