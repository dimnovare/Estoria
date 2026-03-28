using Estoria.Api.Extensions;
using Estoria.Application.DTOs.Properties;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Public;

[ApiController]
[Route("api/properties")]
public class PropertiesController : ControllerBase
{
    private readonly PropertyService _svc;

    public PropertiesController(PropertyService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] PropertyFilterDto filter,
        int page = 1,
        int pageSize = 12,
        CancellationToken ct = default)
    {
        var lang = HttpContext.GetLanguage();
        return Ok(await _svc.GetListAsync(lang, filter, page, pageSize, ct));
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured(
        int count = 6,
        CancellationToken ct = default)
    {
        var lang = HttpContext.GetLanguage();
        return Ok(await _svc.GetFeaturedAsync(lang, count, ct));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct = default)
    {
        var lang = HttpContext.GetLanguage();
        var result = await _svc.GetBySlugAsync(slug, lang, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
