using Estoria.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Public;

[ApiController]
[Route("api/site-settings")]
[AllowAnonymous]
public class SiteSettingsController : ControllerBase
{
    private readonly SiteSettingService _svc;

    public SiteSettingsController(SiteSettingService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok(await _svc.GetAllAsync(publicOnly: true, ct));

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key, CancellationToken ct = default)
    {
        if (!SiteSettingService.PublicKeys.Contains(key))
            return NotFound();

        var result = await _svc.GetByKeyAsync(key, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
