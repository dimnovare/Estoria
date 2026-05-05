using Estoria.Application.DTOs.Newsletter.Campaigns;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/newsletter")]
[Authorize]
public class AdminNewsletterController : ControllerBase
{
    private readonly NewsletterService _svc;

    public AdminNewsletterController(NewsletterService svc) => _svc = svc;

    [HttpGet("subscribers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSubscribers(
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await _svc.GetSubscribersAsync(page, pageSize, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Unsubscribe(Guid id, CancellationToken ct = default)
    {
        await _svc.UnsubscribeAsync(id, ct);
        return NoContent();
    }

    // ── Campaigns ─────────────────────────────────────────────────────────────

    [HttpGet("campaigns")]
    [Authorize(Roles = "Admin,Marketing")]
    public async Task<IActionResult> GetCampaigns(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await _svc.GetCampaignsAsync(page, pageSize, ct));
    }

    [HttpGet("campaigns/{id:guid}")]
    [Authorize(Roles = "Admin,Marketing")]
    public async Task<IActionResult> GetCampaign(Guid id, CancellationToken ct = default)
    {
        var c = await _svc.GetCampaignByIdAsync(id, ct);
        return c is null ? NotFound() : Ok(c);
    }

    /// <summary>
    /// Synchronous send-now. Brokerage subscriber lists are small enough that
    /// the request will complete in seconds; if list size grows we'll move
    /// to a Hangfire-backed flow. Rate-limited so a misclick doesn't fan out
    /// duplicate blasts.
    /// </summary>
    [HttpPost("campaigns/send-now")]
    [Authorize(Roles = "Admin,Marketing")]
    [EnableRateLimiting("newsletter-send")]
    public async Task<IActionResult> SendNow(
        [FromBody] NewsletterCampaignSendDto dto,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var id = await _svc.SendCampaignNowAsync(dto.Subject, dto.BodyHtml, dto.Language, ct);
        var campaign = await _svc.GetCampaignByIdAsync(id, ct);
        return Ok(campaign);
    }
}
