using Estoria.Application.Interfaces;
using Estoria.Application.Services;
using ImageMagick;
using ImageMagick.Drawing;
using Microsoft.AspNetCore.Hosting;
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
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MagickImageProcessingService> _logger;

    public MagickImageProcessingService(
        IFileStorageService storage,
        SiteSettingService settings,
        IWebHostEnvironment env,
        ILogger<MagickImageProcessingService> logger)
    {
        _storage  = storage;
        _settings = settings;
        _env      = env;
        _logger   = logger;
    }

    public async Task<List<ImageVariant>> ProcessPropertyImageAsync(
        Stream originalStream,
        string baseKey,
        bool watermark,
        CancellationToken ct = default)
    {
        // Watermark is the AND of the caller's request and the global toggle —
        // either side can disable. Text + image path come from SiteSettings so
        // marketing can edit them without a deploy. The image path takes
        // precedence; if the file is missing we fall back to the text mark.
        var globalEnabled = await _settings.GetBoolAsync("watermark.enabled", true, ct);
        var effectiveWatermark = watermark && globalEnabled;
        var watermarkText      = await _settings.GetStringAsync("watermark.text",       "ESTORIA",                       ct);
        var watermarkImagePath = await _settings.GetStringAsync("watermark.image_path", "/watermarks/estoria-mark.png", ct);

        // Resolve the image once — Magick can compose from a single decoded
        // copy if we re-load per variant, but this keeps the I/O outside the
        // per-variant hot path and lets us know upfront whether to fall back.
        var resolvedWatermarkPath = ResolveWatermarkPath(watermarkImagePath);

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
                if (effectiveWatermark) ApplyWatermark(img, resolvedWatermarkPath, watermarkText, fallbackFontSize: 14, fallbackPlacement: WatermarkPlacement.BottomRight);
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
                if (effectiveWatermark) ApplyWatermark(img, resolvedWatermarkPath, watermarkText, fallbackFontSize: 32, fallbackPlacement: WatermarkPlacement.Center);
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
                if (effectiveWatermark) ApplyWatermark(img, resolvedWatermarkPath, watermarkText, fallbackFontSize: 48, fallbackPlacement: WatermarkPlacement.Center);
                img.Format  = MagickFormat.WebP;
                img.Quality = 90;
            },
            ct: ct));

        return variants;
    }

    // ── Watermark composite (image-first, text fallback) ──────────────────────
    //
    // Operator note: to backfill existing variants with the new logo
    // watermark, run reprocess on each PropertyImage. SQL convenience for the
    // bulk case (then enqueue Hangfire jobs separately, since Postgres can't
    // call into the .NET worker directly):
    //
    //   UPDATE "PropertyImages" SET "ProcessingStatus" = 0;
    //
    // The Hangfire enqueue is operator-driven via /admin/property-images/{id}/reprocess
    // — this is intentional, not auto, so a bad watermark pass doesn't churn
    // the entire library on accidental config flips.

    /// <summary>
    /// Resolve the watermark image path against wwwroot. Returns null when
    /// the configured path is empty or the file isn't present on disk —
    /// callers fall back to the text watermark in that case.
    /// </summary>
    private string? ResolveWatermarkPath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath)) return null;

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot)) return null;

        var fullPath = Path.Combine(webRoot, configuredPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            _logger.LogWarning(
                "WATERMARK_MISSING path={Path} — falling back to text watermark",
                fullPath);
            return null;
        }
        return fullPath;
    }

    /// <summary>
    /// Composite the configured watermark image, sized proportional to the
    /// variant (1/6th width, capped at 200px) and anchored bottom-right with
    /// a 20px inset. Falls through to <see cref="DrawWatermark"/> when the
    /// image path didn't resolve, preserving the legacy text-watermark
    /// behavior so nothing degrades silently when an admin clears the path
    /// or the file goes missing.
    /// </summary>
    private void ApplyWatermark(
        MagickImage img,
        string? watermarkImagePath,
        string fallbackText,
        int fallbackFontSize,
        WatermarkPlacement fallbackPlacement)
    {
        if (watermarkImagePath is null)
        {
            DrawWatermark(img, fallbackText, fallbackPlacement, fallbackFontSize);
            return;
        }

        try
        {
            using var watermark = new MagickImage(watermarkImagePath);

            // Size to ~1/6 of the base image's width, capped at 200px so a
            // very large source doesn't get a giant mark. Width=0 lets
            // Magick recompute height to preserve aspect ratio.
            var targetWidth = (uint)Math.Min((int)img.Width / 6, 200);
            if (targetWidth < 32) targetWidth = 32;
            watermark.Resize(targetWidth, 0);

            // Enforce ~55% opacity even if the source PNG is fully opaque.
            // Multiply on the alpha channel scales whatever transparency
            // already exists in the source down by the same factor.
            watermark.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, 0.55);

            // Bottom-right with a 20px inset for every variant. Centered
            // composites looked too busy on photo backgrounds in v1.
            img.Composite(watermark, Gravity.Southeast, 20, 20, CompositeOperator.Over);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "WATERMARK_COMPOSITE_FAIL path={Path} — falling back to text",
                watermarkImagePath);
            DrawWatermark(img, fallbackText, fallbackPlacement, fallbackFontSize);
        }
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
