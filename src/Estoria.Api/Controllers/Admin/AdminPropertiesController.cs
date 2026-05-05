using Estoria.Application.DTOs.Properties;
using Estoria.Application.Interfaces;
using Estoria.Application.Services;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/properties")]
[Authorize(Roles = "Admin")]
public class AdminPropertiesController : ControllerBase
{
    private static readonly HashSet<string> _allowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private const long MaxBytes = 10 * 1024 * 1024;

    private readonly PropertyService _svc;
    private readonly IAppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly ICurrentUserService _currentUser;
    private readonly IBackgroundJobClient _jobs;

    public AdminPropertiesController(
        PropertyService svc,
        IAppDbContext db,
        IFileStorageService storage,
        ICurrentUserService currentUser,
        IBackgroundJobClient jobs)
    {
        _svc         = svc;
        _db          = db;
        _storage     = storage;
        _currentUser = currentUser;
        _jobs        = jobs;
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

        var uploaded = new List<PropertyImage>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            if (file.Length > MaxBytes)
                return BadRequest($"'{file.FileName}' exceeds the 10 MB limit.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                return BadRequest($"File type '{ext}' is not allowed.");

            // Originals land in the PRIVATE bucket; they're never publicly
            // URL-able. The processing job picks the row up by id and writes
            // the watermarked variants to the public bucket.
            await using var stream = file.OpenReadStream();
            var originalKey = await _storage.UploadPrivateAsync(
                stream,
                file.FileName,
                file.ContentType,
                folder: $"properties/originals/{id}",
                ct);

            var image = new PropertyImage
            {
                PropertyId       = id,
                OriginalKey      = originalKey,
                Url              = string.Empty,    // populated when processing finishes
                SortOrder        = nextSort++,
                IsCover          = property.Images.Count == 0 && nextSort == 1,
                ProcessingStatus = ImageProcessingStatus.Pending,
            };

            _db.PropertyImages.Add(image);
            uploaded.Add(image);
        }

        await _db.SaveChangesAsync(ct);

        // Enqueue per-row processing. The job is idempotent and survives
        // reprocessing — a failed run flips the row to Failed without
        // throwing back into Hangfire's retry loop.
        foreach (var image in uploaded)
        {
            _jobs.Enqueue<IImageProcessingJob>(j => j.ProcessAsync(image.Id));
        }

        // Emit a history row per uploaded image. LogPropertyEventAsync swallows
        // its own failures, so a degraded history doesn't break the upload.
        foreach (var image in uploaded)
        {
            await _svc.LogPropertyEventAsync(
                id,
                PropertyEventType.ImageAdded,
                prev: null,
                next: new { ImageId = image.Id, image.OriginalKey, image.IsCover },
                userId: _currentUser.UserId,
                ct: ct);
        }

        // Return enough for the admin UI to render Pending placeholders. The
        // variant URLs come back null until the Hangfire run completes; the
        // frontend polls the property record to flip the state.
        var payload = uploaded.Select(image => new
        {
            image.Id,
            image.SortOrder,
            image.IsCover,
            image.ProcessingStatus,
            ThumbUrl  = (string?)null,
            MediumUrl = (string?)null,
            LargeUrl  = (string?)null,
            Url       = (string?)null,
        });

        return Ok(payload);
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

        // Best-effort cleanup of every URL/key we know about. Failures here
        // shouldn't block the row removal — orphaned objects are recoverable
        // (cron sweep, manual cleanup) but a stuck row blocks the UI.
        foreach (var url in new[] { image.Url, image.ThumbUrl, image.MediumUrl, image.LargeUrl })
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                try { await _storage.DeleteAsync(url, ct); } catch { /* see comment above */ }
            }
        }

        if (!string.IsNullOrWhiteSpace(image.OriginalKey))
        {
            try { await _storage.DeletePrivateAsync(image.OriginalKey, ct); } catch { /* see comment above */ }
        }

        _db.PropertyImages.Remove(image);
        await _db.SaveChangesAsync(ct);

        await _svc.LogPropertyEventAsync(
            id,
            PropertyEventType.ImageRemoved,
            prev: new { ImageId = image.Id, image.Url, image.OriginalKey },
            next: null,
            userId: _currentUser.UserId,
            ct: ct);

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
