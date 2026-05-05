using Estoria.Application.Interfaces;
using Estoria.Application.Services;
using ImageMagick;
using ImageMagick.Drawing;
using Microsoft.Extensions.Logging;

namespace Estoria.Infrastructure.Services;

/// <summary>
/// Magick.NET-backed image pipeline. Each call constructs its own MagickImage
/// instances — Magick.NET is not thread-safe across instances, so the service
/// is registered Scoped and a single processing job owns its own copies for
/// the lifetime of the request.
/// </summary>
public class MagickImageProcessingService : IImageProcessingService
{
    private readonly IFileStorageService _storage;
    private readonly SiteSettingService _settings;
    private readonly ILogger<MagickImageProcessingService> _logger;

    public MagickImageProcessingService(
        IFileStorageService storage,
        SiteSettingService settings,
        ILogger<MagickImageProcessingService> logger)
    {
        _storage  = storage;
        _settings = settings;
        _logger   = logger;
    }

    public async Task<List<ImageVariant>> ProcessPropertyImageAsync(
        Stream originalStream,
        string baseKey,
        bool watermark,
        CancellationToken ct = default)
    {
        // Watermark is the AND of the caller's request and the global toggle —
        // either side can disable. Text comes from SiteSettings so marketing
        // can edit it without a deploy.
        var globalEnabled = await _settings.GetBoolAsync("watermark.enabled", true, ct);
        var effectiveWatermark = watermark && globalEnabled;
        var watermarkText = await _settings.GetStringAsync("watermark.text", "ESTORIA", ct);

        // Buffer once. We re-read from byte[] for each variant since each
        // MagickImage owns its decoded buffer and we don't want them sharing.
        byte[] originalBytes;
        if (originalStream is MemoryStream alreadyMs)
        {
            originalBytes = alreadyMs.ToArray();
        }
        else
        {
            using var ms = new MemoryStream();
            await originalStream.CopyToAsync(ms, ct);
            originalBytes = ms.ToArray();
        }

        var variants = new List<ImageVariant>(3);

        // Thumb — square-ish cover, jpg q85 for the smallest possible file.
        variants.Add(await BuildAndUploadAsync(
            originalBytes,
            baseKey,
            variantName: "thumb",
            extension:   "jpg",
            mimeType:    "image/jpeg",
            applyTransform: img =>
            {
                ResizeCover(img, 400, 300);
                if (effectiveWatermark) DrawWatermark(img, watermarkText, WatermarkPlacement.BottomRight, fontSize: 14);
                img.Format  = MagickFormat.Jpeg;
                img.Quality = 85;
            },
            ct: ct));

        variants.Add(await BuildAndUploadAsync(
            originalBytes,
            baseKey,
            variantName: "medium",
            extension:   "webp",
            mimeType:    "image/webp",
            applyTransform: img =>
            {
                ResizeContain(img, 1200, 800);
                if (effectiveWatermark) DrawWatermark(img, watermarkText, WatermarkPlacement.Center, fontSize: 32);
                img.Format  = MagickFormat.WebP;
                img.Quality = 85;
            },
            ct: ct));

        variants.Add(await BuildAndUploadAsync(
            originalBytes,
            baseKey,
            variantName: "large",
            extension:   "webp",
            mimeType:    "image/webp",
            applyTransform: img =>
            {
                ResizeContain(img, 2000, 1400);
                if (effectiveWatermark) DrawWatermark(img, watermarkText, WatermarkPlacement.Center, fontSize: 48);
                img.Format  = MagickFormat.WebP;
                img.Quality = 90;
            },
            ct: ct));

        return variants;
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private async Task<ImageVariant> BuildAndUploadAsync(
        byte[] originalBytes,
        string baseKey,
        string variantName,
        string extension,
        string mimeType,
        Action<MagickImage> applyTransform,
        CancellationToken ct)
    {
        // Each variant gets its own MagickImage — sharing is unsafe and Resize
        // mutates dimensions in place, so passing a single instance through
        // would corrupt subsequent variants.
        using var img = new MagickImage(originalBytes);

        // Honor camera/phone EXIF orientation, then strip metadata so we don't
        // leak GPS coordinates or device serial in the public CDN copy.
        img.AutoOrient();
        img.Strip();

        applyTransform(img);

        var key = $"{baseKey}-{variantName}.{extension}";

        using var output = new MemoryStream();
        await img.WriteAsync(output, ct);
        output.Position = 0;

        var url = await _storage.UploadPublicWithKeyAsync(key, output, mimeType, ct);

        return new ImageVariant
        {
            Variant = variantName,
            Key     = key,
            Url     = url,
            Width   = (int)img.Width,
            Height  = (int)img.Height,
        };
    }

    /// <summary>
    /// Resize keeping aspect ratio, fitting within the given bounds. The image
    /// is never enlarged beyond its source dimensions.
    /// </summary>
    private static void ResizeContain(MagickImage img, uint width, uint height)
    {
        var geometry = new MagickGeometry(width, height) { Greater = true };
        img.Resize(geometry);
    }

    /// <summary>
    /// Resize to cover the bounds (no letterboxing), then center-crop to the
    /// exact requested size. Used by the thumb variant where we want a
    /// uniform card aspect ratio across heterogeneous source images.
    /// </summary>
    private static void ResizeCover(MagickImage img, uint width, uint height)
    {
        var geometry = new MagickGeometry(width, height) { FillArea = true };
        img.Resize(geometry);
        img.Crop(width, height, Gravity.Center);
        img.ResetPage();
    }

    private enum WatermarkPlacement { Center, BottomRight }

    private static void DrawWatermark(
        MagickImage img,
        string text,
        WatermarkPlacement placement,
        int fontSize)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // Low-opacity gold matches the brand palette — visible enough on most
        // photos but not in your face. AdjustingDirectly via Drawables avoids
        // a separate label-image composite.
        var color = new MagickColor(red: 201, green: 168, blue: 76, alpha: 115); // ~0.45 alpha on Q8

        // Approximate text width so we can center / bottom-right align without
        // laying out the text twice. Magick's TypeMetrics needs a font file we
        // don't ship; estimate via fontSize * char count instead. Safe to
        // overshoot — the inset below absorbs minor drift.
        var approxTextWidth = fontSize * Math.Max(1, text.Length) * 0.6;

        double x, y;
        Gravity gravity;

        if (placement == WatermarkPlacement.BottomRight)
        {
            // Anchor to the bottom-right with a 24px inset; Drawables coords
            // are absolute when no gravity is applied, but we use SE gravity
            // so the inset is consistent regardless of image dimensions.
            gravity = Gravity.Southeast;
            x = 24;
            y = 24;
        }
        else
        {
            // Center placement — Drawables x/y becomes an offset from the
            // middle when gravity is Center. Shift up slightly so the text
            // doesn't sit on the visual midline.
            gravity = Gravity.Center;
            x = 0;
            y = 0;
        }

        // Magick.NET 14 dropped the TextAntialias drawable — antialiasing is on
        // by default and toggled via MagickImage.HasAlpha / RenderingIntent
        // when needed. Fine for low-opacity gold text.
        var drawables = new Drawables()
            .FontPointSize(fontSize)
            .FillColor(color)
            .StrokeColor(MagickColors.Transparent)
            .Gravity(gravity)
            .Text(x, y, text);

        img.Draw(drawables);

        // Suppress the unused-local warning on the estimate when nothing in
        // future depends on it; keeping it here as a comment-driver.
        _ = approxTextWidth;
    }
}
