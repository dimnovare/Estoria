using Estoria.Application.DTOs.CRM.Contacts;
using Estoria.Application.Services;
using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

/// <summary>
/// CRM Contacts (people in the pipeline). Read open to any authenticated user;
/// write access enforced inside ContactService via AuthorizationGuard
/// (Admin / Agent only; Editor / Marketing are read-only).
/// </summary>
[ApiController]
[Route("api/admin/contacts")]
[Authorize]
public class AdminContactsController : ControllerBase
{
    private readonly ContactService _svc;

    public AdminContactsController(ContactService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? search    = null,
        [FromQuery] string? tag       = null,
        [FromQuery] ContactSource? source = null,
        [FromQuery] Guid? agentId     = null,
        [FromQuery] int page          = 1,
        [FromQuery] int pageSize      = 50,
        CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        return Ok(await _svc.GetListAsync(search, tag, source, agentId, page, pageSize, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/deals")]
    public async Task<IActionResult> GetDeals(Guid id, CancellationToken ct = default)
        => Ok(await _svc.GetDealsForContactAsync(id, ct));

    [HttpGet("{id:guid}/activities")]
    public async Task<IActionResult> GetActivities(Guid id, CancellationToken ct = default)
        => Ok(await _svc.GetActivitiesForContactAsync(id, ct));

    /// <summary>GDPR-style export: contact + deals + activities + notes.</summary>
    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken ct = default)
        => Ok(await _svc.ExportAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ContactWriteDto dto,
        CancellationToken ct = default)
    {
        var id = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ContactWriteDto dto,
        CancellationToken ct = default)
    {
        await _svc.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
