using System.Text;
using Estoria.Application.Interfaces;
using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Api.Controllers.Public;

/// <summary>
/// Public XML feeds — sitemap (and later: RSS, JSON-LD listing exports).
/// Browser-cached for an hour so crawlers don't hammer Postgres on every
/// fetch; we don't bother with stale-while-revalidate yet because the cost
/// of a fresh build is low.
/// </summary>
[ApiController]
[Route("feeds")]
[AllowAnonymous]
public class FeedsController : ControllerBase
{
    private const string PublicBaseUrl = "https://estoria.estate";

    private readonly IAppDbContext _db;

    public FeedsController(IAppDbContext db) => _db = db;

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken ct = default)
    {
        var sb = new StringBuilder(8 * 1024);
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Static landing pages — these don't change often, so a weekly hint
        // is plenty for crawlers to refresh.
        string[] staticPaths = { "/", "/properties", "/about", "/services",
                                 "/team", "/contact", "/careers", "/blog", "/privacy" };
        foreach (var p in staticPaths)
        {
            sb.Append("  <url><loc>").Append(PublicBaseUrl).Append(p)
              .Append("</loc><changefreq>weekly</changefreq></url>")
              .AppendLine();
        }

        // Active properties only — Draft/Sold/Rented/Archived shouldn't be
        // discoverable from the sitemap. lastmod helps crawlers prioritize
        // newly edited listings.
        var properties = await _db.Properties
            .AsNoTracking()
            .Where(p => p.Status == PropertyStatus.Active)
            .Select(p => new { p.Slug, p.UpdatedAt })
            .ToListAsync(ct);

        foreach (var p in properties)
        {
            sb.Append("  <url><loc>").Append(PublicBaseUrl).Append("/property/").Append(p.Slug)
              .Append("</loc><lastmod>").Append(p.UpdatedAt.ToString("yyyy-MM-dd"))
              .Append("</lastmod><changefreq>weekly</changefreq></url>")
              .AppendLine();
        }

        // Published blog posts — drafts and scheduled posts stay out of the
        // crawl index until they actually go live.
        var posts = await _db.BlogPosts
            .AsNoTracking()
            .Where(b => b.Status == BlogPostStatus.Published)
            .Select(b => new { b.Slug, b.UpdatedAt })
            .ToListAsync(ct);

        foreach (var b in posts)
        {
            sb.Append("  <url><loc>").Append(PublicBaseUrl).Append("/blog/").Append(b.Slug)
              .Append("</loc><lastmod>").Append(b.UpdatedAt.ToString("yyyy-MM-dd"))
              .Append("</lastmod><changefreq>monthly</changefreq></url>")
              .AppendLine();
        }

        sb.AppendLine("</urlset>");
        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }
}
