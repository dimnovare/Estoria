using Estoria.Application.DTOs.CRM.Activities;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/activities")]
[Authorize]
public class AdminActivitiesController : ControllerBase
{
    private readonly ActivityService _svc;

    public AdminActivitiesController(ActivityService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? dealId    = null,
        [FromQuery] Guid? contactId = null,
        [FromQuery] Guid? userId    = null,
        CancellationToken ct = default)
        => Ok(await _svc.GetListAsync(dealId, contactId, userId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ActivityWriteDto dto,
        CancellationToken ct = default)
    {
        var id = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ActivityWriteDto dto,
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
