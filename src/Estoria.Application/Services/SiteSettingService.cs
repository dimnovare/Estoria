using System.Globalization;
using Estoria.Application.DTOs.CMS;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

public class SiteSettingService
{
    public static readonly HashSet<string> PublicKeys = new(StringComparer.Ordinal)
    {
        "stats.years_experience",
        "stats.satisfaction_percent",
        "contact.email",
        "contact.phone",
        "contact.address",
        "contact.hours",
        "social.facebook",
        "social.instagram",
        "social.linkedin",
        "legal.company_name",
        "legal.registry_code",
    };

    /// <summary>
    /// Keys whose value is user-facing copy and should differ between EE/EN/RU.
    /// Everything else (toggles, URLs, numeric stats, registry codes) stays
    /// single-valued — translating them would just add per-language drift
    /// for no reader benefit.
    /// </summary>
    public static readonly HashSet<string> TranslatableKeys = new(StringComparer.Ordinal)
    {
        "contact.hours",
        "contact.address",
    };

    private readonly IAppDbContext _db;

    public SiteSettingService(IAppDbContext db) => _db = db;

    /// <summary>
    /// Returns every setting. When <paramref name="lang"/> is supplied, keys in
    /// <see cref="TranslatableKeys"/> resolve to the per-language translation
    /// row; missing translations fall back to the base Value so the public
    /// site never blanks out on an un-translated key.
    /// </summary>
    public async Task<List<SiteSettingDto>> GetAllAsync(
        bool publicOnly = false,
        Language? lang  = null,
        CancellationToken ct = default)
    {
        var query = _db.SiteSettings.AsNoTracking().AsQueryable();
        if (publicOnly) query = query.Where(s => PublicKeys.Contains(s.Key));

        // Tracking off; we read translations as a side projection. Single
        // round-trip: lift translations alongside the parent in one query.
        var rows = await query
            .Include(s => s.Translations)
            .OrderBy(s => s.Key)
            .ToListAsync(ct);

        return rows.Select(s => new SiteSettingDto
        {
            Key       = s.Key,
            Value     = ResolveValue(s, lang),
            ValueType = s.ValueType,
        }).ToList();
    }

    public async Task<SiteSettingDto?> GetByKeyAsync(
        string key,
        Language? lang = null,
        CancellationToken ct = default)
    {
        var s = await _db.SiteSettings
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Key == key, ct);
        if (s is null) return null;

        return new SiteSettingDto
        {
            Key       = s.Key,
            Value     = ResolveValue(s, lang),
            ValueType = s.ValueType,
        };
    }

    /// <summary>
    /// Admin-detail shape. For translatable keys, returns every translation
    /// row so the form can edit each language tab; for non-translatable
    /// keys, the translations list is empty and Value is authoritative.
    /// </summary>
    public async Task<SiteSettingAdminDetailDto?> GetAdminDetailByKeyAsync(
        string key, CancellationToken ct = default)
    {
        var s = await _db.SiteSettings
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Key == key, ct);
        if (s is null) return null;

        return new SiteSettingAdminDetailDto
        {
            Key            = s.Key,
            Value          = s.Value,
            ValueType      = s.ValueType,
            IsTranslatable = TranslatableKeys.Contains(s.Key),
            Translations   = s.Translations
                .OrderBy(t => t.Language)
                .Select(t => new SiteSettingTranslationDto
                {
                    Language = t.Language,
                    Value    = t.Value,
                })
                .ToList(),
        };
    }

    public async Task UpsertAsync(
        string key, string? value, SettingValueType type, CancellationToken ct = default)
    {
        var existing = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key, ct);

        if (existing is null)
        {
            _db.SiteSettings.Add(new SiteSetting
            {
                Key       = key,
                Value     = value,
                ValueType = type,
            });
        }
        else
        {
            existing.Value = value;
            existing.ValueType = type;
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Upserts a per-language translation row. The setting must exist and
    /// must be in <see cref="TranslatableKeys"/>; anything else is rejected
    /// to keep the schema honest.
    /// </summary>
    public async Task UpsertTranslationAsync(
        string key, Language lang, string value, CancellationToken ct = default)
    {
        if (!TranslatableKeys.Contains(key))
            throw new InvalidOperationException(
                $"SiteSetting '{key}' is not translatable. Add it to TranslatableKeys to enable per-language values.");

        var setting = await _db.SiteSettings
            .Include(s => s.Translations)
            .FirstOrDefaultAsync(s => s.Key == key, ct)
            ?? throw new KeyNotFoundException($"SiteSetting '{key}' not found.");

        var existing = setting.Translations.FirstOrDefault(t => t.Language == lang);
        if (existing is null)
        {
            setting.Translations.Add(new SiteSettingTranslation
            {
                SiteSettingId = setting.Id,
                Language      = lang,
                Value         = value,
            });
        }
        else
        {
            existing.Value = value;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        var existing = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (existing is null) return false;

        _db.SiteSettings.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue, CancellationToken ct = default)
    {
        var raw = await _db.SiteSettings
            .AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            ? n
            : defaultValue;
    }

    public async Task<string> GetStringAsync(string key, string defaultValue, CancellationToken ct = default)
    {
        var raw = await _db.SiteSettings
            .AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        return string.IsNullOrEmpty(raw) ? defaultValue : raw;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken ct = default)
    {
        var raw = await _db.SiteSettings
            .AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        return bool.TryParse(raw, out var b) ? b : defaultValue;
    }

    /// <summary>
    /// Picks the right value for the requested language: for translatable
    /// keys with a matching translation, the translation; otherwise the
    /// base Value (so an un-translated key still renders a sensible string).
    /// </summary>
    private static string? ResolveValue(SiteSetting s, Language? lang)
    {
        if (lang is null || !TranslatableKeys.Contains(s.Key))
            return s.Value;

        var match = s.Translations.FirstOrDefault(t => t.Language == lang.Value);
        return match?.Value ?? s.Value;
    }
}
