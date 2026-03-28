using Estoria.Application.DTOs.Team;
using Estoria.Application.Interfaces;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/team")]
// TODO: [Authorize(Roles = "Admin")]
public class AdminTeamController : ControllerBase
{
    private static readonly HashSet<string> _allowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp"];

    private const long MaxBytes = 10 * 1024 * 1024;

    private readonly TeamService _svc;
    private readonly IAppDbContext _db;
    private readonly IFileStorageService _storage;

    public AdminTeamController(
        TeamService svc,
        IAppDbContext db,
        IFileStorageService storage)
    {
        _svc     = svc;
        _db      = db;
        _storage = storage;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok(await _svc.GetAllAdminAsync(Domain.Enums.Language.En, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        // TeamService exposes GetBySlugAsync; provide Id-based lookup via full admin list
        var all = await _svc.GetAllAdminAsync(Domain.Enums.Language.En, ct);
        var result = all.FirstOrDefault(m => m.Id == id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTeamMemberDto dto,
        CancellationToken ct = default)
    {
        var id = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTeamMemberDto dto,
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

    // ── Photo ─────────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/photo")]
    public async Task<IActionResult> UploadPhoto(
        Guid id,
        IFormFile file,
        CancellationToken ct = default)
    {
        var member = await _db.TeamMembers
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (member is null) return NotFound();

        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (file.Length > MaxBytes)
            return BadRequest("File exceeds the 10 MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(ext))
            return BadRequest($"File type '{ext}' is not allowed.");

        // Delete old photo if present
        if (!string.IsNullOrWhiteSpace(member.PhotoUrl))
            await _storage.DeleteAsync(member.PhotoUrl, ct);

        await using var stream = file.OpenReadStream();
        member.PhotoUrl = await _storage.UploadAsync(
            stream, file.FileName, file.ContentType, "team", ct);

        await _db.SaveChangesAsync(ct);
        return Ok(new { url = member.PhotoUrl });
    }
}
