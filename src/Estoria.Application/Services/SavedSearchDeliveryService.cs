using Estoria.Application.DTOs.SavedSearches;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Estoria.Application.Services;

/// <summary>
/// Hangfire worker that walks active saved-searches at a given frequency,
/// finds matching properties published since each one's LastSentAt, and
/// dispatches a digest. Gated by the <c>savedsearches.auto_send</c>
/// SiteSetting — when off the worker computes the recipient list and logs
/// the would-send count without actually emailing, which lets us validate
/// matching logic in staging without spamming inboxes.
/// </summary>
public class SavedSearchDeliveryService
{
    private readonly IAppDbContext _db;
    private readonly IEmailService _email;
    private readonly SavedSearchService _searchSvc;
    private readonly SiteSettingService _settings;
    private readonly IConfiguration _config;
    private readonly ILogger<SavedSearchDeliveryService> _logger;

    public SavedSearchDeliveryService(
        IAppDbContext db,
        IEmailService email,
        SavedSearchService searchSvc,
        SiteSettingService settings,
        IConfiguration config,
        ILogger<SavedSearchDeliveryService> logger)
    {
        _db        = db;
        _email     = email;
        _searchSvc = searchSvc;
        _settings  = settings;
        _config    = config;
        _logger    = logger;
    }

    /// <summary>
    /// Hangfire entrypoint. Processes every active row at <paramref name="frequency"/>.
    /// </summary>
    public async Task ProcessAsync(SavedSearchFrequency frequency, CancellationToken ct = default)
    {
        var autoSend = await _settings.GetBoolAsync("savedsearches.auto_send", false, ct);

        var rows = await _db.SavedSearches
            .Where(s => s.IsActive && s.Frequency == frequency)
            .ToListAsync(ct);

        var totalDispatched = 0;
        foreach (var search in rows)
        {
            try
            {
                var dispatched = await ProcessOneAsync(search, autoSend, ct);
                if (dispatched) totalDispatched++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "SAVED_SEARCH_RUN_FAIL id={Id} email={Email}",
                    search.Id, search.Email);
            }
        }

        _logger.LogInformation(
            "SAVED_SEARCH_RUN frequency={Frequency} candidates={Candidates} dispatched={Dispatched} autoSend={AutoSend}",
            frequency, rows.Count, totalDispatched, autoSend);
    }

    /// <summary>
    /// Manual force-send used by the admin panel. Bypasses the
    /// <c>savedsearches.auto_send</c> switch on the assumption that an admin
    /// pressing the button wants the email to go out right now regardless.
    /// </summary>
    public async Task<int> ForceSendAsync(Guid id, CancellationToken ct = default)
    {
        var search = await _db.SavedSearches.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new KeyNotFoundException($"SavedSearch {id} not found.");

        await ProcessOneAsync(search, autoSend: true, ct: ct);
        return search.LastResultsCount;
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private async Task<bool> ProcessOneAsync(SavedSearch search, bool autoSend, CancellationToken ct)
    {
        var filter = SavedSearchService.DeserializeFilter(search.FilterJson);
        var sinceUtc = search.LastSentAt ?? DateTime.UtcNow.AddDays(-7);

        var matches = await FindMatchesAsync(filter, sinceUtc, ct);

        // Always update LastSentAt + count so the next run looks at a fresh
        // window. Saving even on dry-run keeps the candidate pool from
        // ballooning when auto-send is off in staging.
        search.LastSentAt       = DateTime.UtcNow;
        search.LastResultsCount = matches.Count;

        if (matches.Count == 0)
        {
            await _db.SaveChangesAsync(ct);
            return false;
        }

        if (!autoSend)
        {
            _logger.LogInformation(
                "SAVED_SEARCH_DRYRUN id={Id} email={Email} matches={Matches} (auto-send disabled)",
                search.Id, search.Email, matches.Count);

            await _db.SaveChangesAsync(ct);
            return false;
        }

        await _email.SendSavedSearchDigestAsync(
            toEmail:        search.Email,
            searchName:     search.Name,
            lang:           search.PreferredLanguage,
            matches:        matches,
            unsubscribeUrl: _searchSvc.BuildUnsubscribeUrl(search.UnsubscribeToken),
            ct:             ct);

        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Finds Active properties created or republished after <paramref name="sinceUtc"/>
    /// that satisfy the saved filter. The match is intentionally inclusive —
    /// imperfect filters still yield digests, which beats silent zero-matches
    /// for early users.
    /// </summary>
    private async Task<List<SavedSearchDigestItem>> FindMatchesAsync(
        SavedSearchFilter f, DateTime sinceUtc, CancellationToken ct)
    {
        var q = _db.Properties
            .AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Images.Where(i => i.IsCover))
            .Include(p => p.Features)
            .Where(p => p.Status == PropertyStatus.Active)
            .Where(p => (p.PublishedAt != null && p.PublishedAt > sinceUtc) || p.CreatedAt > sinceUtc);

        if (f.Type.HasValue)        q = q.Where(p => p.PropertyType    == f.Type.Value);
        if (f.Transaction.HasValue) q = q.Where(p => p.TransactionType == f.Transaction.Value);
        if (f.MinPrice.HasValue)    q = q.Where(p => p.Price >= f.MinPrice.Value);
        if (f.MaxPrice.HasValue)    q = q.Where(p => p.Price <= f.MaxPrice.Value);
        if (f.MinSize.HasValue)     q = q.Where(p => p.Size  >= f.MinSize.Value);
        if (f.MaxSize.HasValue)     q = q.Where(p => p.Size  <= f.MaxSize.Value);
        if (f.MinRooms.HasValue)    q = q.Where(p => p.Rooms != null && p.Rooms >= f.MinRooms.Value);
        if (f.MaxRooms.HasValue)    q = q.Where(p => p.Rooms != null && p.Rooms <= f.MaxRooms.Value);

        if (!string.IsNullOrWhiteSpace(f.City))
        {
            var city = f.City.ToLower();
            q = q.Where(p => p.Translations.Any(t => t.City.ToLower().Contains(city)));
        }
        if (!string.IsNullOrWhiteSpace(f.District))
        {
            var district = f.District.ToLower();
            q = q.Where(p => p.Translations.Any(t =>
                t.District != null && t.District.ToLower().Contains(district)));
        }
        if (f.Features.Count > 0)
        {
            // Properties that include EVERY requested feature. Each Contains
            // becomes a separate EXISTS — selective enough at this scale.
            foreach (var feature in f.Features)
                q = q.Where(p => p.Features.Any(pf => pf.Feature == feature));
        }

        // Cap the digest size; nobody reads a 200-item email.
        var rows = await q
            .OrderByDescending(p => p.PublishedAt)
            .Take(20)
            .ToListAsync(ct);

        var publicBase = _config["PublicSite:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("PUBLIC_SITE_BASE_URL")
            ?? "https://estoria.estate";

        return rows.Select(p =>
        {
            var t = p.Translations.FirstOrDefault(x => x.Language == Language.En)
                 ?? p.Translations.FirstOrDefault();
            return new SavedSearchDigestItem
            {
                Title         = t?.Title ?? string.Empty,
                City          = t?.City  ?? string.Empty,
                Price         = p.Price,
                Currency      = p.Currency,
                Url           = $"{publicBase.TrimEnd('/')}/properties/{p.Slug}",
                CoverImageUrl = p.Images.FirstOrDefault(i => i.IsCover)?.Url,
            };
        }).ToList();
    }
}
