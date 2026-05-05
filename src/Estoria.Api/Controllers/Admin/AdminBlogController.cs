using Estoria.Application.DTOs.Blog;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/blog")]
[Authorize(Roles = "Admin")]
public class AdminBlogController : ControllerBase
{
    private readonly BlogService _svc;

    public AdminBlogController(BlogService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _svc.GetAllAdminAsync(page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _svc.GetByIdAdminAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBlogPostDto dto,
        CancellationToken ct = default)
    {
        var id = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBlogPostDto dto,
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
}
