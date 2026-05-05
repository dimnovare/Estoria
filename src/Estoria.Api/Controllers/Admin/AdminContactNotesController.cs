using Estoria.Application.DTOs.CRM.Notes;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/contact-notes")]
[Authorize]
public class AdminContactNotesController : ControllerBase
{
    private readonly ContactNoteService _svc;

    public AdminContactNotesController(ContactNoteService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetForContact(
        [FromQuery] Guid contactId,
        CancellationToken ct = default)
        => Ok(await _svc.GetForContactAsync(contactId, ct));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ContactNoteWriteDto dto,
        CancellationToken ct = default)
    {
        var id = await _svc.CreateAsync(dto, ct);
        return Created(string.Empty, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ContactNoteWriteDto dto,
        CancellationToken ct = default)
    {
        await _svc.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/pin")]
    public async Task<IActionResult> SetPinned(
        Guid id,
        [FromBody] bool isPinned,
        CancellationToken ct = default)
    {
        await _svc.SetPinnedAsync(id, isPinned, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
