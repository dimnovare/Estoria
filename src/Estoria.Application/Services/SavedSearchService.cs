using System.Security.Cryptography;
using System.Text.Json;
using Estoria.Application.Common;
using Estoria.Application.DTOs.SavedSearches;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Estoria.Application.Services;

/// <summary>
/// Saved-search subscription management. Anonymous-friendly create + opaque
/// token-based unsubscribe; admin list/filter/force-send/delete. The actual
/// digest dispatch lives in <see cref="SavedSearchDeliveryService"/>; this
/// service only owns the row.
/// </summary>
public class SavedSearchService
{
    private static readonly JsonSerializerOptions FilterJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly IAppDbContext _db;
    private readonly IEmailService _email;
    private readonly AuditService _audit;
    private readonly IConfiguration _config;
    private readonly ILogger<SavedSearchService> _logger;

    public SavedSearchService(
        IAppDbContext db,
        IEmailService email,
        AuditService audit,
        IConfiguration config,
        ILogger<SavedSearchService> logger)
    {
        _db     = db;
        _email  = email;
        _audit  = audit;
        _config = config;
        _logger = logger;
    }

    // ── Public ────────────────────────────────────────────────────────────────

    public async Task<Guid> CreateAsync(SavedSearchCreateDto dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        // Opportunistically link to an existing CRM contact when the email
        // already exists. Doesn't require one — that's the whole point of
        // anonymous saved-searches.
        var contactId = await _db.Contacts
            .AsNoTracking()
            .Where(c => c.Email != null && c.Email.ToLower() == email)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);

        var entity = new SavedSearch
        {
            Email             = email,
            ContactId         = contactId,
            Name              = string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name.Trim(),
            PreferredLanguage = dto.PreferredLanguage,
            FilterJson        = JsonSerializer.Serialize(dto.Filter, FilterJsonOptions),
            Frequency         = dto.Frequency,
            UnsubscribeToken  = GenerateToken(),
        };

        _db.SavedSearches.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "SavedSearch.Create",
            entityType: nameof(SavedSearch),
            entityId: entity.Id,
            details: new { entity.Email, entity.Frequency, ContactLinked = contactId is not null },
            ct: ct);

        // Confirmation email — best effort, don't fail the create if delivery
        // wobbles. ResendEmailService already logs RESEND_FAIL for visibility.
        try
        {
            await _email.SendSavedSearchConfirmationAsync(
                toEmail:        entity.Email,
                searchName:     entity.Name,
                lang:           entity.PreferredLanguage,
                unsubscribeUrl: BuildUnsubscribeUrl(entity.UnsubscribeToken),
                ct:             ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "SAVED_SEARCH_CONFIRM_FAIL id={Id} email={Email}",
                entity.Id, entity.Email);
        }

        return entity.Id;
    }

    /// <summary>
    /// Flips IsActive=false for the row identified by an opaque unsubscribe
    /// token. Returns null when the token doesn't match — controllers translate
    /// that into 404 so we don't leak whether a token ever existed.
    /// </summary>
    public async Task<Language?> UnsubscribeAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var entity = await _db.SavedSearches.FirstOrDefaultAsync(s => s.UnsubscribeToken == token, ct);
        if (entity is null) return null;

        if (entity.IsActive)
        {
            entity.IsActive = false;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(
                "SavedSearch.Unsubscribe",
                entityType: nameof(SavedSearch),
                entityId: entity.Id,
                details: new { entity.Email },
                ct: ct);
        }

        return entity.PreferredLanguage;
    }

    // ── Admin ─────────────────────────────────────────────────────────────────

    public async Task<List<SavedSearchDto>> GetAllAsync(
        SavedSearchFrequency? frequency = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var q = _db.SavedSearches.AsNoTracking().AsQueryable();
        if (frequency.HasValue) q = q.Where(s => s.Frequency == frequency.Value);
        if (isActive.HasValue)  q = q.Where(s => s.IsActive == isActive.Value);

        var rows = await q.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<SavedSearchDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.SavedSearches.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        return entity is null ? null : ToDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.SavedSearches.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new KeyNotFoundException($"SavedSearch {id} not found.");

        _db.SavedSearches.Remove(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "SavedSearch.Delete",
            entityType: nameof(SavedSearch),
            entityId: id,
            ct: ct);
    }

    // ── Helpers shared with the delivery worker ───────────────────────────────

    public static SavedSearchFilter DeserializeFilter(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SavedSearchFilter>(json, FilterJsonOptions)
                ?? new SavedSearchFilter();
        }
        catch
        {
            return new SavedSearchFilter();
        }
    }

    public string BuildUnsubscribeUrl(string token)
    {
        var baseUrl = _config["PublicSite:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("PUBLIC_SITE_BASE_URL")
            ?? "https://estoria.estate";

        // Trim trailing slash so the concat doesn't double up. Frontend route
        // is /unsubscribe/{token} (handled by SavedSearchController.Unsubscribe).
        return $"{baseUrl.TrimEnd('/')}/api/saved-searches/unsubscribe/{token}";
    }

    private static SavedSearchDto ToDto(SavedSearch s) => new()
    {
        Id                = s.Id,
        Email             = s.Email,
        ContactId         = s.ContactId,
        Name              = s.Name,
        PreferredLanguage = s.PreferredLanguage,
        Filter            = DeserializeFilter(s.FilterJson),
        Frequency         = s.Frequency,
        LastSentAt        = s.LastSentAt,
        LastResultsCount  = s.LastResultsCount,
        IsActive          = s.IsActive,
        CreatedAt         = s.CreatedAt,
    };

    private static string GenerateToken()
    {
        // 32 random bytes → 43-char URL-safe base64. Comfortably under the
        // 64-char column cap and unguessable for unsubscribe links.
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
