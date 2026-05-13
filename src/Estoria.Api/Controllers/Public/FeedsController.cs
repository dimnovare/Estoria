using System.Text;
using Estoria.Application.Interfaces;
using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Api.Controllers.Public;

/// <summary>
/// Public XML feeds — sitemap (and later: RSS, JSON-LD listing exports).
///
/// Route is the root-level /sitemap.xml so search engines find it without
/// a sitemap directive in robots.txt (though adding one there too is wise).
/// Cached for an hour; fresh build cost is cheap relative to crawler load.
/// </summary>
[ApiController]
[Route("")]          // combined with [HttpGet("sitemap.xml")] → /sitemap.xml
[AllowAnonymous]
public class FeedsController : ControllerBase
{
    private const string BaseUrl = "https://estoria.estate";

    private readonly IAppDbContext _db;

    public FeedsController(IAppDbContext db) => _db = db;

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken ct = default)
    {
        var sb = new StringBuilder(16 * 1024);
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // ── Static pages ──────────────────────────────────────────────────────
        var staticPages = new (string Path, string Freq, string Priority)[]
        {
            ("/",           "weekly",  "1.0"),
            ("/properties", "daily",   "0.9"),
            ("/about",      "monthly", "0.8"),
            ("/team",       "monthly", "0.7"),
            ("/services",   "monthly", "0.7"),
            ("/blog",       "weekly",  "0.7"),
            ("/careers",    "monthly", "0.5"),
            ("/contact",    "monthly", "0.6"),
            ("/privacy",    "yearly",  "0.3"),
        };
        foreach (var (path, freq, priority) in staticPages)
            AppendUrl(sb, $"{BaseUrl}{path}", freq, priority);

        // ── Active (live) properties ──────────────────────────────────────────
        // Draft / Sold / Rented / Archived listings are excluded so crawlers
        // don't index pages that redirect or 404. PropertyStatus.Active = 1.
        var properties = await _db.Properties
            .AsNoTracking()
            .Where(p => p.Status == PropertyStatus.Active)
            .Select(p => new { p.Slug, p.UpdatedAt })
            .ToListAsync(ct);

        foreach (var p in properties)
            AppendUrl(sb, $"{BaseUrl}/properties/{p.Slug}", "weekly", "0.8",
                p.UpdatedAt.ToString("yyyy-MM-dd"));

        // ── Published blog posts ──────────────────────────────────────────────
        var posts = await _db.BlogPosts
            .AsNoTracking()
            .Where(b => b.Status == BlogPostStatus.Published)
            .Select(b => new { b.Slug, b.UpdatedAt })
            .ToListAsync(ct);

        foreach (var b in posts)
            AppendUrl(sb, $"{BaseUrl}/blog/{b.Slug}", "monthly", "0.6",
                b.UpdatedAt.ToString("yyyy-MM-dd"));

        // ── Active team members ───────────────────────────────────────────────
        var team = await _db.TeamMembers
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => new { t.Slug })
            .ToListAsync(ct);

        foreach (var t in team)
            AppendUrl(sb, $"{BaseUrl}/team/{t.Slug}", "monthly", "0.5");

        sb.AppendLine("</urlset>");
        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    private static void AppendUrl(
        StringBuilder sb, string loc,
        string changefreq, string priority,
        string? lastmod = null)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{loc}</loc>");
        if (lastmod is not null)
            sb.AppendLine($"    <lastmod>{lastmod}</lastmod>");
        sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
        sb.AppendLine($"    <priority>{priority}</priority>");
        sb.AppendLine("  </url>");
    }
}
