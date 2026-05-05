using Estoria.Application.Services;
using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

/// <summary>
/// Admin surface for inspecting and operating saved-search subscriptions.
/// Force-send and delete are Admin-only; list/get accept any authenticated
/// caller so agents can debug their own contacts' subscriptions.
/// </summary>
[ApiController]
[Route("api/admin/saved-searches")]
[Authorize]
public class AdminSavedSearchesController : ControllerBase
{
    private readonly SavedSearchService _svc;
    private readonly SavedSearchDeliveryService _delivery;

    public AdminSavedSearchesController(
        SavedSearchService svc,
        SavedSearchDeliveryService delivery)
    {
        _svc      = svc;
        _delivery = delivery;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] SavedSearchFrequency? frequency = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
        => Ok(await _svc.GetAllAsync(frequency, isActive, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/force-send")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ForceSend(Guid id, CancellationToken ct = default)
    {
        var count = await _delivery.ForceSendAsync(id, ct);
        return Ok(new { matchesFound = count });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
