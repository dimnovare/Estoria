using Estoria.Application.DTOs.CRM.Tasks;
using Estoria.Application.Services;
using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

/// <summary>
/// User-managed reminders / todos. Read open to any authenticated user; write
/// access enforced inside <see cref="TaskService"/> via AuthorizationGuard
/// (Admin / Agent only; Editor / Marketing are read-only).
/// </summary>
[ApiController]
[Route("api/admin/tasks")]
[Authorize]
public class AdminTasksController : ControllerBase
{
    private readonly TaskService _svc;

    public AdminTasksController(TaskService svc) => _svc = svc;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _svc.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Filter-rich admin list. Every parameter optional — no-arg returns
    /// the most recent slice across assignees. Used by /admin/tasks.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? assignedToId        = null,
        [FromQuery] AppTaskStatus? status     = null,
        [FromQuery] AppTaskPriority? priority = null,
        [FromQuery] bool? overdue             = null,
        [FromQuery] bool? hasReminder         = null,
        [FromQuery] DateTime? dueBefore       = null,
        [FromQuery] DateTime? dueAfter        = null,
        [FromQuery] Guid? contactId           = null,
        [FromQuery] Guid? dealId              = null,
        [FromQuery] Guid? propertyId          = null,
        [FromQuery] int page                  = 1,
        [FromQuery] int pageSize              = 50,
        CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var result = await _svc.GetListAsync(
            assignedToId, status, priority, overdue, hasReminder,
            dueBefore, dueAfter, contactId, dealId, propertyId,
            page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Tasks assigned to the caller. <paramref name="dueWithin"/> accepts a
    /// duration string ("7d", "12h", "2.05:00:00"); when null, no time bound.
    /// <paramref name="overdue"/> = true narrows further to Pending+past-due.
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] AppTaskStatus? status = null,
        [FromQuery] string? dueWithin     = null,
        [FromQuery] bool? overdue         = null,
        CancellationToken ct = default)
    {
        TimeSpan? window = null;
        if (!string.IsNullOrWhiteSpace(dueWithin))
            window = ParseDueWithin(dueWithin);

        return Ok(await _svc.GetMineAsync(status, window, overdue, ct));
    }

    [HttpGet("by-contact/{contactId:guid}")]
    public async Task<IActionResult> GetForContact(Guid contactId, CancellationToken ct = default)
        => Ok(await _svc.GetForContactAsync(contactId, ct));

    [HttpGet("by-deal/{dealId:guid}")]
    public async Task<IActionResult> GetForDeal(Guid dealId, CancellationToken ct = default)
        => Ok(await _svc.GetForDealAsync(dealId, ct));

    [HttpGet("by-property/{propertyId:guid}")]
    public async Task<IActionResult> GetForProperty(Guid propertyId, CancellationToken ct = default)
        => Ok(await _svc.GetForPropertyAsync(propertyId, ct));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] AppTaskWriteDto dto,
        CancellationToken ct = default)
    {
        var id = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AppTaskWriteDto dto,
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

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct = default)
    {
        await _svc.CompleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Optimistic toggle endpoint. Sibling to Complete that accepts any
    /// target status — reopen-to-Pending, mark Cancelled, etc.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetTaskStatusDto dto,
        CancellationToken ct = default)
    {
        await _svc.SetStatusAsync(id, dto.Status, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reschedule")]
    public async Task<IActionResult> Reschedule(
        Guid id,
        [FromBody] RescheduleDto dto,
        CancellationToken ct = default)
    {
        await _svc.RescheduleAsync(id, dto, ct);
        return NoContent();
    }

    private static TimeSpan ParseDueWithin(string raw)
    {
        // Compact form first ("7d", "12h", "30m") so callers don't have to
        // remember .NET's TimeSpan format. Falls through to TimeSpan.Parse for
        // power users / scripts.
        var trimmed = raw.Trim().ToLowerInvariant();
        if (trimmed.Length > 1)
        {
            var num = trimmed[..^1];
            switch (trimmed[^1])
            {
                case 'd': if (int.TryParse(num, out var d)) return TimeSpan.FromDays(d); break;
                case 'h': if (int.TryParse(num, out var h)) return TimeSpan.FromHours(h); break;
                case 'm': if (int.TryParse(num, out var m)) return TimeSpan.FromMinutes(m); break;
            }
        }
        return TimeSpan.Parse(raw);
    }
}
