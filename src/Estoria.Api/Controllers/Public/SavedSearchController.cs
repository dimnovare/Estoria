using System.Net;
using Estoria.Application.DTOs.SavedSearches;
using Estoria.Application.Services;
using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Estoria.Api.Controllers.Public;

/// <summary>
/// Public surface for anonymous saved-search subscriptions. POST is rate-
/// limited under the existing "newsletter" policy so signup spam can't flood
/// the table. Unsubscribe is a GET with an opaque token so it works from
/// inside an email client without JS.
/// </summary>
[ApiController]
[Route("api/saved-searches")]
public class SavedSearchController : ControllerBase
{
    private readonly SavedSearchService _svc;

    public SavedSearchController(SavedSearchService svc) => _svc = svc;

    [HttpPost]
    [EnableRateLimiting("newsletter")]
    public async Task<IActionResult> Create(
        [FromBody] SavedSearchCreateDto dto,
        CancellationToken ct = default)
    {
        var id = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(null, new { id }, new { id });
    }

    /// <summary>
    /// One-click unsubscribe. Returns a tiny HTML page so the recipient sees a
    /// confirmation when they click the link from their email client.
    /// </summary>
    [HttpGet("unsubscribe/{token}")]
    public async Task<IActionResult> Unsubscribe(string token, CancellationToken ct = default)
    {
        var lang = await _svc.UnsubscribeAsync(token, ct);

        var (title, message) = (lang ?? ResolveAcceptLanguage()) switch
        {
            Language.Et => ("Tellimus tühistatud",
                "Teie salvestatud otsingu tellimus on edukalt tühistatud."),
            Language.Ru => ("Вы отписались",
                "Подписка на сохранённый поиск успешно отменена."),
            _           => ("Unsubscribed",
                "Your saved-search subscription has been cancelled."),
        };

        // Lightweight inline HTML — no template engine required, and works
        // even if the recipient's mail client opens the link in a barebones
        // browser. Self-contained styling keeps it readable on dark themes.
        var html = $$"""
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8" />
                <title>{{WebUtility.HtmlEncode(title)}}</title>
                <style>
                  body { font-family: system-ui, -apple-system, sans-serif; max-width: 480px; margin: 8rem auto; padding: 1rem; color: #222; }
                  h1 { font-size: 1.5rem; margin-bottom: 0.5rem; }
                  p { line-height: 1.55; }
                </style>
              </head>
              <body>
                <h1>{{WebUtility.HtmlEncode(title)}}</h1>
                <p>{{WebUtility.HtmlEncode(message)}}</p>
              </body>
            </html>
            """;

        // 200 even when the token didn't match — we don't leak token validity.
        return Content(html, "text/html");
    }

    private Language ResolveAcceptLanguage()
    {
        var header = Request.Headers.AcceptLanguage.ToString().ToLowerInvariant();
        if (header.StartsWith("et")) return Language.Et;
        if (header.StartsWith("ru")) return Language.Ru;
        return Language.En;
    }
}
