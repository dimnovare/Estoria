using Estoria.Application.Interfaces;
using Estoria.Application.Services;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Api.Controllers.Admin;

/// <summary>
/// Per-image admin operations that don't fit on
/// <see cref="AdminPropertiesController"/>: re-enqueue a failed/stale
/// processing run, or fetch a short-lived presigned URL for the original
/// stored in the private bucket.
/// </summary>
[ApiController]
[Route("api/admin/property-images")]
[Authorize(Roles = "Admin")]
public class AdminPropertyImagesController : ControllerBase
{
    /// <summary>5 minutes is enough for an admin click → save dialog round-trip.</summary>
    private static readonly TimeSpan PresignedUrlTtl = TimeSpan.FromMinutes(5);

    private readonly IAppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IBackgroundJobClient _jobs;
    private readonly AuditService _audit;

    public AdminPropertyImagesController(
        IAppDbContext db,
        IFileStorageService storage,
        IBackgroundJobClient jobs,
        AuditService audit)
    {
        _db      = db;
        _storage = storage;
        _jobs    = jobs;
        _audit   = audit;
    }

    /// <summary>
    /// Re-enqueues the processing job. Usable for failed rows or for forcing a
    /// rebuild after watermark settings change.
    /// </summary>
    [HttpPost("{id:guid}/reprocess")]
    public async Task<IActionResult> Reprocess(Guid id, CancellationToken ct = default)
    {
        var image = await _db.PropertyImages.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (image is null) return NotFound();

        if (string.IsNullOrWhiteSpace(image.OriginalKey))
        {
            // Legacy row from before the two-bucket pipeline — there's nothing
            // to reprocess from, so we surface the limitation rather than
            // failing silently.
            return BadRequest("This image has no original to reprocess from.");
        }

        // Reset the row to Pending so the UI shows the spinner again. The
        // job itself will flip to Processing on its first action.
        image.ProcessingStatus = ImageProcessingStatus.Pending;
        image.ProcessingError  = null;
        await _db.SaveChangesAsync(ct);

        _jobs.Enqueue<IImageProcessingJob>(j => j.ProcessAsync(image.Id));

        await _audit.LogAsync(
            "PropertyImage.Reprocess",
            entityType: nameof(PropertyImage),
            entityId: image.Id,
            details: new { image.OriginalKey },
            ct: ct);

        return Accepted(new { image.Id, image.ProcessingStatus });
    }

    /// <summary>
    /// Returns a short-lived presigned URL for the original stored in the
    /// private bucket. Audit-logged so we can answer "who downloaded the
    /// non-watermarked original" later.
    /// </summary>
    [HttpGet("{id:guid}/original-url")]
    public async Task<IActionResult> GetOriginalUrl(Guid id, CancellationToken ct = default)
    {
        var image = await _db.PropertyImages
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (image is null) return NotFound();

        if (string.IsNullOrWhiteSpace(image.OriginalKey))
            return NotFound("No original is on file for this image.");

        var url = await _storage.GetPresignedUrlAsync(image.OriginalKey, PresignedUrlTtl, ct);
        var expiresAt = DateTime.UtcNow.Add(PresignedUrlTtl);

        await _audit.LogAsync(
            "PropertyImage.DownloadOriginal",
            entityType: nameof(PropertyImage),
            entityId: image.Id,
            details: new { image.OriginalKey, ExpiresAt = expiresAt },
            ct: ct);

        return Ok(new { url, expiresAt });
    }
}
