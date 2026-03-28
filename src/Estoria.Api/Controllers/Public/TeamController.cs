using Estoria.Api.Extensions;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Public;

[ApiController]
[Route("api/team")]
public class TeamController : ControllerBase
{
    private readonly TeamService _svc;

    public TeamController(TeamService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var lang = HttpContext.GetLanguage();
        return Ok(await _svc.GetAllActiveAsync(lang, ct));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct = default)
    {
        var lang = HttpContext.GetLanguage();
        var result = await _svc.GetBySlugAsync(slug, lang, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
