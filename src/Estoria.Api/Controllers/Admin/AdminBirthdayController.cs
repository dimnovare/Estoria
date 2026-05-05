using Estoria.Application.DTOs.CRM.Birthday;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

/// <summary>
/// Admin surface for the birthday automation: list upcoming birthdays, edit
/// the email template, and manually trigger the daily greeting run for
/// staging/QA. The recurring Hangfire job (registered in Program.cs) calls
/// the same SendBirthdayGreetingsAsync at 08:00 UTC.
/// </summary>
[ApiController]
[Route("api/admin/birthday")]
[Authorize]
public class AdminBirthdayController : ControllerBase
{
    private readonly BirthdayService _svc;

    public AdminBirthdayController(BirthdayService svc) => _svc = svc;

    /// <summary>Contacts with a DOB whose next anniversary falls within N days.</summary>
    [HttpGet("upcoming")]
    public async Task<IActionResult> Upcoming(
        [FromQuery] int days = 14,
        CancellationToken ct = default)
        => Ok(await _svc.GetUpcomingAsync(days, ct));

    [HttpGet("template")]
    public async Task<IActionResult> GetTemplate(CancellationToken ct = default)
    {
        var t = await _svc.GetTemplateAsync(ct);
        return t is null ? NotFound() : Ok(t);
    }

    [HttpPut("template")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTemplate(
        [FromBody] BirthdayTemplateUpsertDto dto,
        CancellationToken ct = default)
    {
        await _svc.UpsertTemplateAsync(dto, ct);
        return NoContent();
    }

    /// <summary>
    /// Manually fires today's birthday run. Useful for verifying the email
    /// integration end-to-end before flipping <c>birthday.auto_send</c> on.
    /// </summary>
    [HttpPost("send-now")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendNow(CancellationToken ct = default)
        => Ok(await _svc.SendBirthdayGreetingsAsync(ct));
}
