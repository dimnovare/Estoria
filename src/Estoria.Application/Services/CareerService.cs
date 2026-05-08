using Estoria.Application.Common;
using Estoria.Application.DTOs.Careers;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

public class CareerService
{
    private readonly IAppDbContext _db;

    public CareerService(IAppDbContext db) => _db = db;

    // -------------------------------------------------------------------------
    // Public
    // -------------------------------------------------------------------------

    public async Task<List<CareerListDto>> GetActiveAsync(
        Language lang, CancellationToken ct = default)
    {
        var postings = await _db.CareerPostings
            .AsNoTracking()
            .Include(cp => cp.Translations)
            .Where(cp => cp.IsActive)
            .OrderByDescending(cp => cp.CreatedAt)
            .ToListAsync(ct);

        return postings.Select(cp => ToListDto(cp, lang)).ToList();
    }

    public async Task<CareerDetailDto?> GetBySlugAsync(
        string slug, Language lang, CancellationToken ct = default)
    {
        var posting = await _db.CareerPostings
            .AsNoTracking()
            .Include(cp => cp.Translations)
            .FirstOrDefaultAsync(cp => cp.Slug == slug && cp.IsActive, ct);

        if (posting is null) return null;

        var t = ResolveTranslation(posting.Translations, lang);
        return new CareerDetailDto
        {
            Id = posting.Id,
            Slug = posting.Slug,
            Title = t?.Title ?? string.Empty,
            Description = t?.Description ?? string.Empty,
            Location = t?.Location,
            IsActive = posting.IsActive
        };
    }

    // -------------------------------------------------------------------------
    // Admin
    // -------------------------------------------------------------------------

    public async Task<List<CareerListDto>> GetAllAdminAsync(
        Language lang, bool includeArchived = false, CancellationToken ct = default)
    {
        var postings = await _db.CareerPostings
            .AsNoTracking()
            .Include(cp => cp.Translations)
            .Where(cp => includeArchived || cp.IsActive)
            .OrderByDescending(cp => cp.CreatedAt)
            .ToListAsync(ct);

        return postings.Select(cp => ToListDto(cp, lang)).ToList();
    }

    public async Task<Guid> CreateAsync(
        CreateCareerDto dto, CancellationToken ct = default)
    {
        var enTitle = (dto.Translations.TryGetValue(Language.En, out var enTrans) && !string.IsNullOrWhiteSpace(enTrans.Title))
            ? enTrans.Title
            : dto.Translations.Values.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Title))?.Title ?? string.Empty;

        if (string.IsNullOrWhiteSpace(enTitle))
            throw new ArgumentException("At least one language must have a title.");

        var baseSlug = SlugHelper.GenerateSlug(enTitle);
        var slug = await SlugHelper.UniqueAsync(baseSlug,
            s => _db.CareerPostings.AnyAsync(x => x.Slug == s, ct));

        var posting = new CareerPosting
        {
            Slug = slug
        };

        foreach (var (lang, trans) in dto.Translations)
        {
            if (string.IsNullOrWhiteSpace(trans.Title)) continue;
            posting.Translations.Add(new CareerPostingTranslation
            {
                CareerPostingId = posting.Id,
                Language = lang,
                Title = trans.Title,
                Description = trans.Description,
                Location = trans.Location
            });
        }

        _db.CareerPostings.Add(posting);
        await _db.SaveChangesAsync(ct);
        return posting.Id;
    }

    public async Task UpdateAsync(
        Guid id, UpdateCareerDto dto, CancellationToken ct = default)
    {
        var posting = await _db.CareerPostings
            .Include(cp => cp.Translations)
            .FirstOrDefaultAsync(cp => cp.Id == id, ct)
            ?? throw new KeyNotFoundException($"CareerPosting {id} not found.");

        var enTitle = dto.Translations.TryGetValue(Language.En, out var enTrans)
            ? enTrans.Title
            : dto.Translations.Values.First().Title;

        var baseSlug = SlugHelper.GenerateSlug(enTitle);
        posting.Slug = await SlugHelper.UniqueAsync(baseSlug,
            s => _db.CareerPostings.AnyAsync(x => x.Slug == s && x.Id != id, ct));

        var dbCtx = (DbContext)_db;
        foreach (var t in posting.Translations.ToList())
            dbCtx.Entry(t).State = EntityState.Detached;
        posting.Translations.Clear();

        await _db.CareerPostingTranslations.Where(t => t.CareerPostingId == id).ExecuteDeleteAsync(ct);

        foreach (var (lang, trans) in dto.Translations)
        {
            if (string.IsNullOrWhiteSpace(trans.Title)) continue;
            posting.Translations.Add(new CareerPostingTranslation
            {
                CareerPostingId = posting.Id,
                Language = lang,
                Title = trans.Title,
                Description = trans.Description,
                Location = trans.Location
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var posting = await _db.CareerPostings.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"CareerPosting {id} not found.");

        posting.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static CareerPostingTranslation? ResolveTranslation(
        List<CareerPostingTranslation> list, Language lang)
        => list.FirstOrDefault(t => t.Language == lang)
           ?? list.FirstOrDefault(t => t.Language == Language.En)
           ?? list.FirstOrDefault();

    private static CareerListDto ToListDto(CareerPosting cp, Language lang)
    {
        var t = ResolveTranslation(cp.Translations, lang);
        return new CareerListDto
        {
            Id = cp.Id,
            Slug = cp.Slug,
            Title = t?.Title ?? string.Empty,
            Location = t?.Location,
            IsActive = cp.IsActive
        };
    }
}
