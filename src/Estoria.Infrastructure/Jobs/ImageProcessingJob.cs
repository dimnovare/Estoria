using Estoria.Application.Interfaces;
using Estoria.Application.Services;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Estoria.Infrastructure.Jobs;

/// <summary>
/// Concrete Hangfire job that runs the image pipeline for a single
/// <see cref="PropertyImage"/> row. Wired in DI as scoped so the underlying
/// AppDbContext + Magick service are fresh per execution.
/// </summary>
public class ImageProcessingJob : IImageProcessingJob
{
    private const int MaxErrorLength = 1000;

    private readonly IAppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IImageProcessingService _processor;
    private readonly AuditService _audit;
    private readonly ILogger<ImageProcessingJob> _logger;

    public ImageProcessingJob(
        IAppDbContext db,
        IFileStorageService storage,
        IImageProcessingService processor,
        AuditService audit,
        ILogger<ImageProcessingJob> logger)
    {
        _db        = db;
        _storage   = storage;
        _processor = processor;
        _audit     = audit;
        _logger    = logger;
    }

    public async Task ProcessAsync(Guid propertyImageId)
    {
        var image = await _db.PropertyImages.FirstOrDefaultAsync(i => i.Id == propertyImageId);
        if (image is null)
        {
            // Caller (or admin) deleted the row before the job picked it up.
            // Drop silently — Hangfire treats this as success.
            _logger.LogInformation("Image processing skipped — row {Id} not found", propertyImageId);
            return;
        }

        if (image.ProcessingStatus == ImageProcessingStatus.Done)
        {
            // Re-enqueued accidentally; nothing to do.
            return;
        }

        image.ProcessingStatus = ImageProcessingStatus.Processing;
        image.ProcessingError  = null;
        await _db.SaveChangesAsync();

        try
        {
            await using var original = await _storage.GetPrivateStreamAsync(image.OriginalKey);

            // Variant prefix mirrors the original layout but lives in the
            // public bucket. We strip the "originals/" segment to keep the
            // public path clean: properties/originals/{p}/{u}.jpg →
            // properties/{p}/{u} for variants.
            var baseKey = BuildVariantBaseKey(image.OriginalKey);

            // Watermark stays on by default for buy-side leakage protection;
            // SiteSettings "watermark.enabled" is the global override.
            var variants = await _processor.ProcessPropertyImageAsync(
                originalStream: original,
                baseKey:        baseKey,
                watermark:      true);

            var thumb  = variants.FirstOrDefault(v => v.Variant == "thumb");
            var medium = variants.FirstOrDefault(v => v.Variant == "medium");
            var large  = variants.FirstOrDefault(v => v.Variant == "large");

            image.ThumbUrl  = thumb?.Url;
            image.MediumUrl = medium?.Url;
            image.LargeUrl  = large?.Url;
            // Url stays the canonical "best available" pointer for legacy
            // consumers — populate from medium since that's the typical
            // hero/listing display size.
            image.Url       = medium?.Url ?? large?.Url ?? thumb?.Url ?? image.Url;
            image.ProcessingStatus = ImageProcessingStatus.Done;
            image.ProcessingError  = null;

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // User-facing copy stays generic. Real exception lands in audit
            // log with the full stack so we can debug without exposing
            // Magick.NET internals to the admin UI.
            image.ProcessingStatus = ImageProcessingStatus.Failed;
            image.ProcessingError  = "Image processing failed, please try a different file.";

            try { await _db.SaveChangesAsync(); }
            catch (Exception saveEx)
            {
                _logger.LogWarning(saveEx,
                    "Failed to persist Failed status for image {Id}", propertyImageId);
            }

            try
            {
                await _audit.LogAsync(
                    "PropertyImage.ProcessingFailed",
                    entityType: nameof(PropertyImage),
                    entityId: propertyImageId,
                    details: new
                    {
                        Error = Truncate(ex.ToString(), MaxErrorLength),
                        image.OriginalKey,
                    });
            }
            catch (Exception auditEx)
            {
                _logger.LogWarning(auditEx,
                    "Failed to write audit row for image processing failure {Id}", propertyImageId);
            }

            _logger.LogWarning(ex,
                "IMAGE_PROCESS_FAIL imageId={ImageId} key={Key}",
                propertyImageId, image.OriginalKey);

            // Swallow — Hangfire would retry forever otherwise. Admin uses
            // the explicit /reprocess endpoint to retry once they've fixed
            // the source.
        }
    }

    private static string BuildVariantBaseKey(string originalKey)
    {
        // Original layout: properties/originals/{propertyId}/{uuid}.{ext}
        // Variant layout : properties/{propertyId}/{uuid}-{variant}.{ext}
        // Strip the "/originals/" segment and the original file extension.
        var withoutOriginals = originalKey.Replace("/originals/", "/");
        var ext = Path.GetExtension(withoutOriginals);
        return string.IsNullOrEmpty(ext)
            ? withoutOriginals
            : withoutOriginals[..^ext.Length];
    }

    private static string Truncate(string s, int max)
        => s.Length > max ? s[..max] : s;
}
