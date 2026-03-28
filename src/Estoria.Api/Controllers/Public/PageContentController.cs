using Estoria.Api.Extensions;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Public;

[ApiController]
[Route("api/pages")]
public class PageContentController : ControllerBase
{
    private readonly PageContentService _svc;

    public PageContentController(PageContentService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var lang = HttpContext.GetLanguage();
        return Ok(await _svc.GetAllAsync(lang, ct));
    }

    [HttpGet("{pageKey}")]
    public async Task<IActionResult> GetByKey(string pageKey, CancellationToken ct = default)
    {
        var lang = HttpContext.GetLanguage();
        var result = await _svc.GetByKeyAsync(pageKey, lang, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
