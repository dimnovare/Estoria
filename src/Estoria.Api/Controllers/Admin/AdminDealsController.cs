using Estoria.Application.DTOs.CRM.Deals;
using Estoria.Application.Services;
using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/deals")]
[Authorize]
public class AdminDealsController : ControllerBase
{
    private readonly DealService _svc;

    public AdminDealsController(DealService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] DealStage? stage   = null,
        [FromQuery] Guid? agentId      = null,
        [FromQuery] Guid? contactId    = null,
        CancellationToken ct = default)
        => Ok(await _svc.GetListAsync(stage, agentId, contactId, ct));

    /// <summary>Kanban-friendly list: deals grouped by Stage. Empty stages still appear.</summary>
    [HttpGet("kanban")]
    public async Task<IActionResult> GetKanban(
        [FromQuery] Guid? agentId = null,
        CancellationToken ct = default)
        => Ok(await _svc.GetKanbanAsync(agentId, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] DealWriteDto dto,
        CancellationToken ct = default)
    {
        var id = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] DealWriteDto dto,
        CancellationToken ct = default)
    {
        await _svc.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    /// <summary>
    /// Atomic stage transition. Won requires actualValue; Lost requires lossReason.
    /// Side-effect: writes a StageChange Activity row tied to the deal.
    /// </summary>
    [HttpPost("{id:guid}/stage")]
    public async Task<IActionResult> ChangeStage(
        Guid id,
        [FromBody] ChangeStageDto dto,
        CancellationToken ct = default)
    {
        await _svc.ChangeStageAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
