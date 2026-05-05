namespace Estoria.Application.Interfaces;

/// <summary>
/// One generated variant returned from <see cref="IImageProcessingService.ProcessPropertyImageAsync"/>.
/// The original is intentionally not represented here — the caller already has
/// its key from the upload step and the original is never publicly URL-able.
/// </summary>
public class ImageVariant
{
    /// <summary>Logical name: "thumb" | "medium" | "large".</summary>
    public string Variant { get; set; } = string.Empty;

    /// <summary>Object key in the public bucket (no scheme, no host).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Full public URL ready for &lt;img src&gt;.</summary>
    public string Url { get; set; } = string.Empty;

    public int Width { get; set; }
    public int Height { get; set; }
}

public interface IImageProcessingService
{
    /// <summary>
    /// Generates resized + (optionally) watermarked variants for a property image.
    /// </summary>
    /// <param name="originalStream">Image bytes. Must be seekable.</param>
    /// <param name="baseKey">
    /// Variant key prefix without suffix or extension —
    /// e.g. <c>properties/{propertyId}/{uuid}</c>. The service appends
    /// <c>-thumb.jpg</c>, <c>-medium.webp</c>, <c>-large.webp</c>.
    /// </param>
    /// <param name="watermark">
    /// When false, the watermark is suppressed regardless of SiteSettings.
    /// When true, the SiteSetting <c>watermark.enabled</c> is the final gate.
    /// </param>
    Task<List<ImageVariant>> ProcessPropertyImageAsync(
        Stream originalStream,
        string baseKey,
        bool watermark,
        CancellationToken ct = default);
}
