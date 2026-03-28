using Estoria.Api.Extensions;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Public;

[ApiController]
[Route("api/careers")]
public class CareersController : ControllerBase
{
    private readonly CareerService _svc;

    public CareersController(CareerService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken ct = default)
    {
        var lang = HttpContext.GetLanguage();
        return Ok(await _svc.GetActiveAsync(lang, ct));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct = default)
    {
        var lang = HttpContext.GetLanguage();
        var result = await _svc.GetBySlugAsync(slug, lang, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
