using Estoria.Application.DTOs.CRM.Birthday;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

/// <summary>
/// Admin surface for the birthday automation: list upcoming birthdays, edit
/// the email template per language, and manually trigger the daily greeting
/// (bulk or single-contact). The recurring Hangfire job (Program.cs) calls
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

    /// <summary>
    /// Returns one entry per Language enum value (Et, En, Ru) — flat array so
    /// the admin UI can bind a language tab per row without conditional logic.
    /// Languages without any saved customization come back with empty strings.
    /// </summary>
    [HttpGet("template")]
    public async Task<IActionResult> GetTemplate(CancellationToken ct = default)
        => Ok(await _svc.GetTranslationsAsync(ct));

    /// <summary>
    /// Single-translation upsert. Body is one BirthdayTemplateTranslationDto;
    /// language is identified by the Language enum on the body, not the URL.
    /// </summary>
    [HttpPut("template")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpsertTemplate(
        [FromBody] BirthdayTemplateTranslationDto dto,
        CancellationToken ct = default)
    {
        await _svc.UpsertTranslationAsync(dto.Language, dto.Subject, dto.BodyHtml, ct);
        return NoContent();
    }

    /// <summary>
    /// Manual fire. Body { contactId? } scopes to one contact when present;
    /// absent body sends to every eligible contact (same rules as the
    /// recurring job).
    /// </summary>
    [HttpPost("send-now")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendNow(
        [FromBody] BirthdaySendRequestDto? dto = null,
        CancellationToken ct = default)
    {
        if (dto?.ContactId is { } id)
            return Ok(await _svc.SendOneAsync(id, ct));

        return Ok(await _svc.SendBirthdayGreetingsAsync(ct));
    }
}
