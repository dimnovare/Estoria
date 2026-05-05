using Estoria.Api.Extensions;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Public;

[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicLookupController : ControllerBase
{
    private readonly PublicLookupService _svc;

    public PublicLookupController(PublicLookupService svc) => _svc = svc;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct = default)
        => Ok(await _svc.GetStatsAsync(ct));

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities(CancellationToken ct = default)
        => Ok(await _svc.GetCitiesAsync(HttpContext.GetLanguage(), ct));

    [HttpGet("property-types")]
    public async Task<IActionResult> GetPropertyTypes(CancellationToken ct = default)
        => Ok(await _svc.GetTypeOptionsAsync(HttpContext.GetLanguage(), ct));
}
