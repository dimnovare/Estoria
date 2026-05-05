using Estoria.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/newsletter")]
[Authorize(Roles = "Admin")]
public class AdminNewsletterController : ControllerBase
{
    private readonly NewsletterService _svc;

    public AdminNewsletterController(NewsletterService svc) => _svc = svc;

    [HttpGet("subscribers")]
    public async Task<IActionResult> GetSubscribers(
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await _svc.GetSubscribersAsync(page, pageSize, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Unsubscribe(Guid id, CancellationToken ct = default)
    {
        await _svc.UnsubscribeAsync(id, ct);
        return NoContent();
    }
}
