using Estoria.Application.DTOs.CMS;
using Estoria.Application.Services;
using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/site-settings")]
[Authorize(Roles = "Admin")]
public class AdminSiteSettingsController : ControllerBase
{
    private readonly SiteSettingService _svc;

    public AdminSiteSettingsController(SiteSettingService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok(await _svc.GetAllAsync(publicOnly: false, ct: ct));

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key, CancellationToken ct = default)
    {
        var result = await _svc.GetByKeyAsync(key, ct);
        return result is null ? NotFound() : Ok(result);
    }

    public class UpsertSiteSettingDto
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public SettingValueType ValueType { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertSiteSettingDto dto, CancellationToken ct = default)
    {
        await _svc.UpsertAsync(dto.Key, dto.Value, dto.ValueType, ct);
        return NoContent();
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Update(
        string key,
        [FromBody] UpdateSiteSettingDto dto,
        CancellationToken ct = default)
    {
        var existing = await _svc.GetByKeyAsync(key, ct);
        if (existing is null) return NotFound();

        await _svc.UpsertAsync(key, dto.Value, existing.ValueType, ct);
        return NoContent();
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key, CancellationToken ct = default)
    {
        var deleted = await _svc.DeleteAsync(key, ct);
        return deleted ? NoContent() : NotFound();
    }
}
