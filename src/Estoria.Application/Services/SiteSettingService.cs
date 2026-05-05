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
    };

    private readonly IAppDbContext _db;

    public SiteSettingService(IAppDbContext db) => _db = db;

    public async Task<List<SiteSettingDto>> GetAllAsync(
        bool publicOnly = false, CancellationToken ct = default)
    {
        var query = _db.SiteSettings.AsNoTracking();

        if (publicOnly)
            query = query.Where(s => PublicKeys.Contains(s.Key));

        var settings = await query
            .OrderBy(s => s.Key)
            .ToListAsync(ct);

        return settings.Select(s => new SiteSettingDto
        {
            Key = s.Key,
            Value = s.Value,
            ValueType = s.ValueType
        }).ToList();
    }

    public async Task<SiteSettingDto?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        var s = await _db.SiteSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key, ct);

        return s is null ? null : new SiteSettingDto
        {
            Key = s.Key,
            Value = s.Value,
            ValueType = s.ValueType
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
                Key = key,
                Value = value,
                ValueType = type
            });
        }
        else
        {
            existing.Value = value;
            existing.ValueType = type;
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
}
