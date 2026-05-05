using Estoria.Application.DTOs.CMS;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/pages")]
[Authorize(Roles = "Admin")]
public class AdminPageContentController : ControllerBase
{
    private readonly PageContentService _svc;

    public AdminPageContentController(PageContentService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok(await _svc.GetAllAdminAsync(ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePageContentDto dto,
        CancellationToken ct = default)
    {
        await _svc.UpdateAsync(id, dto, ct);
        return NoContent();
    }
}
