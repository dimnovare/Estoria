using Estoria.Application.Common;
using Estoria.Application.DTOs.Services;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

public class OfferedServiceService
{
    private readonly IAppDbContext _db;

    public OfferedServiceService(IAppDbContext db) => _db = db;

    // -------------------------------------------------------------------------
    // Public
    // -------------------------------------------------------------------------

    public async Task<List<ServiceListDto>> GetAllActiveAsync(
        Language lang, CancellationToken ct = default)
    {
        var services = await _db.Services
            .AsNoTracking()
            .Include(s => s.Translations)
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        return services.Select(s => ToListDto(s, lang)).ToList();
    }

    // -------------------------------------------------------------------------
    // Admin
    // -------------------------------------------------------------------------

    public async Task<List<ServiceListDto>> GetAllAdminAsync(
        Language lang, CancellationToken ct = default)
    {
        var services = await _db.Services
            .AsNoTracking()
            .Include(s => s.Translations)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        return services.Select(s => ToListDto(s, lang)).ToList();
    }

    public async Task<Guid> CreateAsync(
        CreateServiceDto dto, CancellationToken ct = default)
    {
        var enName = dto.Translations.TryGetValue(Language.En, out var enTrans)
            ? enTrans.Name
            : dto.Translations.Values.First().Name;

        var service = new Service
        {
            Slug = SlugHelper.GenerateSlug(enName),
            IconName = dto.IconName,
            SortOrder = dto.SortOrder
        };

        foreach (var (lang, trans) in dto.Translations)
            service.Translations.Add(new ServiceTranslation
            {
                ServiceId = service.Id,
                Language = lang,
                Name = trans.Name,
                Description = trans.Description,
                PriceInfo = trans.PriceInfo
            });

        _db.Services.Add(service);
        await _db.SaveChangesAsync(ct);
        return service.Id;
    }

    public async Task UpdateAsync(
        Guid id, UpdateServiceDto dto, CancellationToken ct = default)
    {
        var service = await _db.Services
            .Include(s => s.Translations)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new KeyNotFoundException($"Service {id} not found.");

        var enName = dto.Translations.TryGetValue(Language.En, out var enTrans)
            ? enTrans.Name
            : dto.Translations.Values.First().Name;

        service.Slug = SlugHelper.GenerateSlug(enName);
        service.IconName = dto.IconName;
        service.SortOrder = dto.SortOrder;

        _db.ServiceTranslations.RemoveRange(service.Translations);
        service.Translations.Clear();

        foreach (var (lang, trans) in dto.Translations)
            service.Translations.Add(new ServiceTranslation
            {
                ServiceId = service.Id,
                Language = lang,
                Name = trans.Name,
                Description = trans.Description,
                PriceInfo = trans.PriceInfo
            });

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var service = await _db.Services.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Service {id} not found.");

        service.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static ServiceTranslation? ResolveTranslation(
        List<ServiceTranslation> list, Language lang)
        => list.FirstOrDefault(t => t.Language == lang)
           ?? list.FirstOrDefault(t => t.Language == Language.En)
           ?? list.FirstOrDefault();

    private static ServiceListDto ToListDto(Service s, Language lang)
    {
        var t = ResolveTranslation(s.Translations, lang);
        return new ServiceListDto
        {
            Id = s.Id,
            Slug = s.Slug,
            IconName = s.IconName,
            Name = t?.Name ?? string.Empty,
            Description = t?.Description ?? string.Empty,
            PriceInfo = t?.PriceInfo
        };
    }
}
