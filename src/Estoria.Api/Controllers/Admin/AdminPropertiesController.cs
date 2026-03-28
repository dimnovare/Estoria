using Estoria.Application.DTOs.Properties;
using Estoria.Application.Interfaces;
using Estoria.Application.Services;
using Estoria.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/properties")]
// TODO: [Authorize(Roles = "Admin")]
public class AdminPropertiesController : ControllerBase
{
    private static readonly HashSet<string> _allowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private const long MaxBytes = 10 * 1024 * 1024;

    private readonly PropertyService _svc;
    private readonly IAppDbContext _db;
    private readonly IFileStorageService _storage;

    public AdminPropertiesController(
        PropertyService svc,
        IAppDbContext db,
        IFileStorageService storage)
    {
        _svc     = svc;
        _db      = db;
        _storage = storage;
    }

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
        [FromBody] CreatePropertyDto dto,
        CancellationToken ct = default)
    {
        var id = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePropertyDto dto,
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

    // ── Images ────────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/images")]
    public async Task<IActionResult> UploadImages(
        Guid id,
        List<IFormFile> files,
        CancellationToken ct = default)
    {
        var property = await _db.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (property is null) return NotFound();

        if (files is null || files.Count == 0)
            return BadRequest("No files provided.");

        var nextSort = property.Images.Count > 0
            ? property.Images.Max(i => i.SortOrder) + 1
            : 0;

        var uploaded = new List<object>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            if (file.Length > MaxBytes)
                return BadRequest($"'{file.FileName}' exceeds the 10 MB limit.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                return BadRequest($"File type '{ext}' is not allowed.");

            await using var stream = file.OpenReadStream();
            var url = await _storage.UploadAsync(stream, file.FileName, file.ContentType, "properties", ct);

            var image = new PropertyImage
            {
                PropertyId = id,
                Url        = url,
                SortOrder  = nextSort++,
                IsCover    = property.Images.Count == 0 && nextSort == 1
            };

            _db.PropertyImages.Add(image);
            uploaded.Add(new { image.Id, image.Url, image.SortOrder, image.IsCover });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(uploaded);
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(
        Guid id,
        Guid imageId,
        CancellationToken ct = default)
    {
        var image = await _db.PropertyImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.PropertyId == id, ct);

        if (image is null) return NotFound();

        await _storage.DeleteAsync(image.Url, ct);
        _db.PropertyImages.Remove(image);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPut("{id:guid}/images/reorder")]
    public async Task<IActionResult> ReorderImages(
        Guid id,
        [FromBody] List<ImageReorderDto> items,
        CancellationToken ct = default)
    {
        var imageIds = items.Select(x => x.Id).ToList();

        var images = await _db.PropertyImages
            .Where(i => i.PropertyId == id && imageIds.Contains(i.Id))
            .ToListAsync(ct);

        foreach (var item in items)
        {
            var image = images.FirstOrDefault(i => i.Id == item.Id);
            if (image is not null)
                image.SortOrder = item.SortOrder;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
