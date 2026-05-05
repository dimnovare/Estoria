using System.Net;
using Estoria.Application.DTOs.Newsletter;
using Estoria.Application.Services;
using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Estoria.Api.Controllers.Public;

[ApiController]
[Route("api/newsletter")]
public class NewsletterController : ControllerBase
{
    private readonly NewsletterService _svc;

    public NewsletterController(NewsletterService svc) => _svc = svc;

    [HttpPost("subscribe")]
    [EnableRateLimiting("newsletter")]
    public async Task<IActionResult> Subscribe(
        [FromBody] SubscribeDto dto,
        CancellationToken ct = default)
    {
        await _svc.SubscribeAsync(dto, ct);
        return Ok();
    }

    /// <summary>
    /// One-click unsubscribe — same shape as the saved-search version. Returns
    /// a tiny localized HTML page so the recipient sees a confirmation when
    /// they click the link from their email client.
    /// </summary>
    [HttpGet("unsubscribe/{token}")]
    public async Task<IActionResult> Unsubscribe(string token, CancellationToken ct = default)
    {
        var lang = await _svc.UnsubscribeByTokenAsync(token, ct);

        var (title, message) = (lang ?? ResolveAcceptLanguage()) switch
        {
            Language.Et => ("Tellimus tühistatud",
                "Teie uudiskirja tellimus on edukalt tühistatud."),
            Language.Ru => ("Вы отписались",
                "Подписка на рассылку успешно отменена."),
            _           => ("Unsubscribed",
                "Your newsletter subscription has been cancelled."),
        };

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
