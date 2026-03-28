using Estoria.Application.DTOs.CMS;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

public class PageContentService
{
    private readonly IAppDbContext _db;

    public PageContentService(IAppDbContext db) => _db = db;

    // -------------------------------------------------------------------------
    // Public
    // -------------------------------------------------------------------------

    public async Task<PageContentDto?> GetByKeyAsync(
        string pageKey, Language lang, CancellationToken ct = default)
    {
        var content = await _db.PageContents
            .AsNoTracking()
            .Include(pc => pc.Translations)
            .FirstOrDefaultAsync(pc => pc.PageKey == pageKey, ct);

        if (content is null) return null;

        var t = ResolveTranslation(content.Translations, lang);
        return new PageContentDto
        {
            Id = content.Id,
            PageKey = content.PageKey,
            Title = t?.Title,
            Body = t?.Body,
            ImageUrl = t?.ImageUrl,
            VideoUrl = t?.VideoUrl
        };
    }

    public async Task<List<PageContentDto>> GetAllAsync(
        Language lang, CancellationToken ct = default)
    {
        var contents = await _db.PageContents
            .AsNoTracking()
            .Include(pc => pc.Translations)
            .OrderBy(pc => pc.PageKey)
            .ToListAsync(ct);

        return contents.Select(pc =>
        {
            var t = ResolveTranslation(pc.Translations, lang);
            return new PageContentDto
            {
                Id = pc.Id,
                PageKey = pc.PageKey,
                Title = t?.Title,
                Body = t?.Body,
                ImageUrl = t?.ImageUrl,
                VideoUrl = t?.VideoUrl
            };
        }).ToList();
    }

    // -------------------------------------------------------------------------
    // Admin
    // -------------------------------------------------------------------------

    public async Task<List<AdminPageContentDto>> GetAllAdminAsync(
        CancellationToken ct = default)
    {
        var contents = await _db.PageContents
            .AsNoTracking()
            .Include(pc => pc.Translations)
            .OrderBy(pc => pc.PageKey)
            .ToListAsync(ct);

        return contents.Select(pc => new AdminPageContentDto
        {
            Id = pc.Id,
            PageKey = pc.PageKey,
            Translations = pc.Translations.ToDictionary(
                t => t.Language,
                t => new PageTranslationDto
                {
                    Title = t.Title,
                    Body = t.Body,
                    ImageUrl = t.ImageUrl,
                    VideoUrl = t.VideoUrl
                })
        }).ToList();
    }

    public async Task UpdateAsync(
        Guid id, UpdatePageContentDto dto, CancellationToken ct = default)
    {
        var content = await _db.PageContents
            .Include(pc => pc.Translations)
            .FirstOrDefaultAsync(pc => pc.Id == id, ct)
            ?? throw new KeyNotFoundException($"PageContent {id} not found.");

        _db.PageContentTranslations.RemoveRange(content.Translations);
        content.Translations.Clear();

        foreach (var (lang, trans) in dto.Translations)
            content.Translations.Add(new PageContentTranslation
            {
                PageContentId = content.Id,
                Language = lang,
                Title = trans.Title,
                Body = trans.Body,
                ImageUrl = trans.ImageUrl,
                VideoUrl = trans.VideoUrl
            });

        await _db.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static PageContentTranslation? ResolveTranslation(
        List<PageContentTranslation> list, Language lang)
        => list.FirstOrDefault(t => t.Language == lang)
           ?? list.FirstOrDefault(t => t.Language == Language.En)
           ?? list.FirstOrDefault();
}
