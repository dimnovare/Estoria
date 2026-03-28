using Estoria.Api.Extensions;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Public;

[ApiController]
[Route("api/blog")]
public class BlogController : ControllerBase
{
    private readonly BlogService _svc;

    public BlogController(BlogService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetList(
        int page = 1,
        int pageSize = 9,
        CancellationToken ct = default)
    {
        var lang = HttpContext.GetLanguage();
        return Ok(await _svc.GetListAsync(lang, page, pageSize, ct));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct = default)
    {
        var lang = HttpContext.GetLanguage();
        var result = await _svc.GetBySlugAsync(slug, lang, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
