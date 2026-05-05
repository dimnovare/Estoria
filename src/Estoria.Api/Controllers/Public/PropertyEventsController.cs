using Estoria.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Public;

/// <summary>
/// Public history feed for a single property. Drives the "Price History"
/// collapsible on the property detail page. Public-safe events only — agent
/// and image churn is filtered out inside <see cref="PropertyService"/>.
/// </summary>
[ApiController]
[Route("api/properties/{slug}/history")]
public class PropertyEventsController : ControllerBase
{
    private readonly PropertyService _svc;

    public PropertyEventsController(PropertyService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetHistory(
        string slug,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var events = await _svc.GetPublicHistoryAsync(slug, limit, ct);
        return events is null ? NotFound() : Ok(events);
    }
}
