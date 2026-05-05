using System.Security.Cryptography;
using Estoria.Application.Common;
using Estoria.Application.DTOs.Newsletter;
using Estoria.Application.DTOs.Newsletter.Campaigns;
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
    private readonly ICurrentUserService _currentUser;

    public NewsletterService(
        IAppDbContext db,
        IEmailService email,
        ILogger<NewsletterService> logger,
        AuditService audit,
        ICurrentUserService currentUser)
    {
        _db          = db;
        _email       = email;
        _logger      = logger;
        _audit       = audit;
        _currentUser = currentUser;
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
            // Backfill the token for legacy rows that pre-dated the
            // AddNewsletterUnsubscribeToken migration.
            if (string.IsNullOrEmpty(existing.UnsubscribeToken))
                existing.UnsubscribeToken = GenerateToken();
        }
        else
        {
            _db.NewsletterSubscribers.Add(new NewsletterSubscriber
            {
                Email            = dto.Email,
                Language         = lang,
                IsActive         = true,
                SubscribedAt     = DateTime.UtcNow,
                UnsubscribeToken = GenerateToken(),
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

    /// <summary>
    /// Public unsubscribe-by-token. Returns the subscriber's preferred
    /// language so the controller can render the confirmation page localized.
    /// Returns null when the token doesn't match — caller renders a generic
    /// localized "unsubscribed" page anyway so we don't leak token validity.
    /// </summary>
    public async Task<Language?> UnsubscribeByTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var subscriber = await _db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.UnsubscribeToken == token, ct);
        if (subscriber is null) return null;

        if (subscriber.IsActive)
        {
            subscriber.IsActive = false;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(
                "Newsletter.UnsubscribeByToken",
                entityType: nameof(NewsletterSubscriber),
                entityId: subscriber.Id,
                details: new { subscriber.Email },
                ct: ct);
        }

        return subscriber.Language;
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

    // -------------------------------------------------------------------------
    // Campaigns
    // -------------------------------------------------------------------------

    public async Task<PagedResult<NewsletterCampaignDto>> GetCampaignsAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.NewsletterCampaigns
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<NewsletterCampaignDto>
        {
            Items      = items.Select(ToCampaignDto).ToList(),
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize,
        };
    }

    public async Task<NewsletterCampaignDto?> GetCampaignByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _db.NewsletterCampaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? null : ToCampaignDto(c);
    }

    /// <summary>
    /// Synchronous send-now: creates the campaign row, dispatches one email
    /// per active subscriber (filtered by language when set), and tallies the
    /// per-recipient outcome on the row. Returns the new campaign id so the
    /// caller can fetch the row for the final tally.
    /// </summary>
    public async Task<Guid> SendCampaignNowAsync(
        string subject,
        string bodyHtml,
        Language? languageFilter,
        CancellationToken ct = default)
    {
        var campaign = new NewsletterCampaign
        {
            Subject        = subject.Trim(),
            BodyHtml       = bodyHtml,
            LanguageFilter = languageFilter,
            Status         = NewsletterCampaignStatus.Sending,
            StartedAt      = DateTime.UtcNow,
            SentByUserId   = _currentUser.UserId ?? Guid.Empty,
        };

        var subscribersQuery = _db.NewsletterSubscribers
            .Where(s => s.IsActive && !string.IsNullOrEmpty(s.Email));
        if (languageFilter.HasValue)
            subscribersQuery = subscribersQuery.Where(s => s.Language == languageFilter.Value);

        var subscribers = await subscribersQuery.ToListAsync(ct);
        campaign.RecipientsCount = subscribers.Count;

        _db.NewsletterCampaigns.Add(campaign);
        await _db.SaveChangesAsync(ct);

        var success = 0;
        var failure = 0;

        foreach (var sub in subscribers)
        {
            // Empty token shouldn't happen post-migration, but if a legacy
            // backfill is missing one, generate it on the fly so the email
            // can still go out with a valid unsubscribe URL.
            if (string.IsNullOrEmpty(sub.UnsubscribeToken))
            {
                sub.UnsubscribeToken = GenerateToken();
                await _db.SaveChangesAsync(ct);
            }

            try
            {
                var ok = await _email.SendNewsletterCampaignAsync(
                    toEmail:          sub.Email,
                    lang:             sub.Language,
                    subject:          campaign.Subject,
                    bodyHtml:         campaign.BodyHtml,
                    unsubscribeToken: sub.UnsubscribeToken,
                    ct:               ct);
                if (ok) success++; else failure++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "NEWSLETTER_CAMPAIGN_FAIL campaignId={CampaignId} email={Email}",
                    campaign.Id, sub.Email);
                failure++;
            }
        }

        campaign.SuccessCount = success;
        campaign.FailureCount = failure;
        campaign.Status       = (failure > 0 && success == 0)
            ? NewsletterCampaignStatus.Failed
            : NewsletterCampaignStatus.Sent;
        campaign.CompletedAt  = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Newsletter.SendCampaign",
            entityType: nameof(NewsletterCampaign),
            entityId: campaign.Id,
            details: new
            {
                campaign.Subject,
                LanguageFilter = languageFilter?.ToString(),
                campaign.RecipientsCount,
                campaign.SuccessCount,
                campaign.FailureCount,
            },
            ct: ct);

        return campaign.Id;
    }

    private static NewsletterCampaignDto ToCampaignDto(NewsletterCampaign c) => new()
    {
        Id              = c.Id,
        Subject         = c.Subject,
        BodyHtml        = c.BodyHtml,
        LanguageFilter  = c.LanguageFilter,
        Status          = c.Status,
        StartedAt       = c.StartedAt,
        CompletedAt     = c.CompletedAt,
        RecipientsCount = c.RecipientsCount,
        SuccessCount    = c.SuccessCount,
        FailureCount    = c.FailureCount,
        SentByUserId    = c.SentByUserId,
        CreatedAt       = c.CreatedAt,
    };

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
