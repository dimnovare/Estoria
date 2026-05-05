using Estoria.Application.Services;
using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

/// <summary>
/// Admin endpoints for inbound public contact-form messages. Distinct from
/// AdminContactsController which manages CRM Contacts (people in the pipeline).
/// </summary>
[ApiController]
[Route("api/admin/contact-messages")]
[Authorize(Roles = "Admin")]
public class AdminContactMessagesController : ControllerBase
{
    private readonly ContactMessageService _svc;

    public AdminContactMessagesController(ContactMessageService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _svc.GetAllAsync(page, pageSize, ct));

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] ContactStatus status,
        CancellationToken ct = default)
    {
        await _svc.UpdateStatusAsync(id, status, ct);
        return NoContent();
    }
}
