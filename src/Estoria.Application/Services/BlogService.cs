using Estoria.Application.Common;
using Estoria.Application.DTOs.Blog;
using Estoria.Application.DTOs.Team;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

public class BlogService
{
    private readonly IAppDbContext _db;

    public BlogService(IAppDbContext db) => _db = db;

    // -------------------------------------------------------------------------
    // Public
    // -------------------------------------------------------------------------

    public async Task<PagedResult<BlogPostListDto>> GetListAsync(
        Language lang, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var query = _db.BlogPosts
            .AsNoTracking()
            .Include(b => b.Translations)
            .Include(b => b.Author).ThenInclude(a => a.Translations)
            .Where(b => b.Status == BlogPostStatus.Published)
            .OrderByDescending(b => b.PublishedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<BlogPostListDto>
        {
            Items = items.Select(b => ToListDto(b, lang)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<BlogPostDetailDto?> GetBySlugAsync(
        string slug, Language lang, CancellationToken ct = default)
    {
        var post = await _db.BlogPosts
            .AsNoTracking()
            .Include(b => b.Translations)
            .Include(b => b.Author).ThenInclude(a => a.Translations)
            .FirstOrDefaultAsync(b => b.Slug == slug && b.Status == BlogPostStatus.Published, ct);

        return post is null ? null : ToDetailDto(post, lang);
    }

    // -------------------------------------------------------------------------
    // Admin
    // -------------------------------------------------------------------------

    public async Task<PagedResult<AdminBlogDetailDto>> GetAllAdminAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.BlogPosts
            .AsNoTracking()
            .Include(b => b.Translations)
            .Include(b => b.Author).ThenInclude(a => a.Translations)
            .OrderByDescending(b => b.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AdminBlogDetailDto>
        {
            Items = items.Select(ToAdminDetailDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminBlogDetailDto?> GetByIdAdminAsync(
        Guid id, CancellationToken ct = default)
    {
        var post = await _db.BlogPosts
            .AsNoTracking()
            .Include(b => b.Translations)
            .Include(b => b.Author).ThenInclude(a => a.Translations)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        return post is null ? null : ToAdminDetailDto(post);
    }

    public async Task<Guid> CreateAsync(
        CreateBlogPostDto dto, CancellationToken ct = default)
    {
        var enTitle = dto.Translations.TryGetValue(Language.En, out var enTrans)
            ? enTrans.Title
            : dto.Translations.Values.First().Title;

        var post = new BlogPost
        {
            Slug = SlugHelper.GenerateSlug(enTitle),
            AuthorId = dto.AuthorId,
            CoverImageUrl = dto.CoverImageUrl
        };

        foreach (var (lang, trans) in dto.Translations)
            post.Translations.Add(new BlogPostTranslation
            {
                BlogPostId = post.Id,
                Language = lang,
                Title = trans.Title,
                Excerpt = trans.Excerpt,
                Content = trans.Content,
                MetaTitle = trans.MetaTitle,
                MetaDescription = trans.MetaDescription
            });

        _db.BlogPosts.Add(post);
        await _db.SaveChangesAsync(ct);
        return post.Id;
    }

    public async Task UpdateAsync(
        Guid id, UpdateBlogPostDto dto, CancellationToken ct = default)
    {
        var post = await _db.BlogPosts
            .Include(b => b.Translations)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new KeyNotFoundException($"BlogPost {id} not found.");

        var enTitle = dto.Translations.TryGetValue(Language.En, out var enTrans)
            ? enTrans.Title
            : dto.Translations.Values.First().Title;

        post.Slug = SlugHelper.GenerateSlug(enTitle);
        post.AuthorId = dto.AuthorId;
        post.CoverImageUrl = dto.CoverImageUrl;

        _db.BlogPostTranslations.RemoveRange(post.Translations);
        post.Translations.Clear();

        foreach (var (lang, trans) in dto.Translations)
            post.Translations.Add(new BlogPostTranslation
            {
                BlogPostId = post.Id,
                Language = lang,
                Title = trans.Title,
                Excerpt = trans.Excerpt,
                Content = trans.Content,
                MetaTitle = trans.MetaTitle,
                MetaDescription = trans.MetaDescription
            });

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var post = await _db.BlogPosts.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"BlogPost {id} not found.");

        _db.BlogPosts.Remove(post);
        await _db.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static BlogPostTranslation? ResolveTranslation(
        List<BlogPostTranslation> list, Language lang)
        => list.FirstOrDefault(t => t.Language == lang)
           ?? list.FirstOrDefault(t => t.Language == Language.En)
           ?? list.FirstOrDefault();

    private static TeamMemberListDto MapAuthor(TeamMember author, Language lang)
    {
        var t = author.Translations.FirstOrDefault(x => x.Language == lang)
                ?? author.Translations.FirstOrDefault(x => x.Language == Language.En)
                ?? author.Translations.FirstOrDefault();
        return new TeamMemberListDto
        {
            Id = author.Id,
            Slug = author.Slug,
            Name = t?.Name ?? string.Empty,
            Role = t?.Role ?? string.Empty,
            PhotoUrl = author.PhotoUrl,
            Phone = author.Phone,
            Email = author.Email,
            Languages = author.Languages
        };
    }

    private static BlogPostListDto ToListDto(BlogPost b, Language lang)
    {
        var t = ResolveTranslation(b.Translations, lang);
        var authorTrans = b.Author.Translations.FirstOrDefault(x => x.Language == lang)
                          ?? b.Author.Translations.FirstOrDefault(x => x.Language == Language.En)
                          ?? b.Author.Translations.FirstOrDefault();
        return new BlogPostListDto
        {
            Id = b.Id,
            Slug = b.Slug,
            Title = t?.Title ?? string.Empty,
            Excerpt = t?.Excerpt,
            CoverImageUrl = b.CoverImageUrl,
            AuthorName = authorTrans?.Name ?? string.Empty,
            AuthorPhotoUrl = b.Author.PhotoUrl,
            PublishedAt = b.PublishedAt
        };
    }

    private static BlogPostDetailDto ToDetailDto(BlogPost b, Language lang)
    {
        var t = ResolveTranslation(b.Translations, lang);
        return new BlogPostDetailDto
        {
            Id = b.Id,
            Slug = b.Slug,
            Title = t?.Title ?? string.Empty,
            Excerpt = t?.Excerpt,
            Content = t?.Content ?? string.Empty,
            MetaTitle = t?.MetaTitle,
            MetaDescription = t?.MetaDescription,
            CoverImageUrl = b.CoverImageUrl,
            PublishedAt = b.PublishedAt,
            Author = MapAuthor(b.Author, lang)
        };
    }

    private static AdminBlogDetailDto ToAdminDetailDto(BlogPost b)
    {
        var authorTrans = b.Author.Translations
            .FirstOrDefault(t => t.Language == Language.En)
            ?? b.Author.Translations.FirstOrDefault();

        return new AdminBlogDetailDto
        {
            Id = b.Id,
            Slug = b.Slug,
            CoverImageUrl = b.CoverImageUrl,
            AuthorId = b.AuthorId,
            AuthorName = authorTrans?.Name ?? string.Empty,
            Status = b.Status,
            PublishedAt = b.PublishedAt,
            CreatedAt = b.CreatedAt,
            Translations = b.Translations.ToDictionary(
                t => t.Language,
                t => new BlogTranslationDto
                {
                    Title = t.Title,
                    Excerpt = t.Excerpt,
                    Content = t.Content,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription
                })
        };
    }
}
