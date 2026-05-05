using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class PropertyImage : BaseEntity
{
    public Guid PropertyId { get; set; }

    /// <summary>
    /// Legacy "best available" URL. Kept for backwards compatibility with code
    /// that hasn't migrated to the variant fields yet (public PropertyService
    /// projections, older frontend calls). On new uploads this is populated
    /// from <see cref="MediumUrl"/> once processing completes.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }

    /// <summary>
    /// Object key in the PRIVATE bucket (no scheme, no host). Never publicly
    /// addressable — admins fetch a presigned URL via the dedicated endpoint.
    /// Empty for legacy rows uploaded before the two-bucket pipeline.
    /// </summary>
    public string OriginalKey { get; set; } = string.Empty;

    public string? ThumbUrl { get; set; }
    public string? MediumUrl { get; set; }
    public string? LargeUrl { get; set; }

    public ImageProcessingStatus ProcessingStatus { get; set; } = ImageProcessingStatus.Pending;

    /// <summary>
    /// Truncated error string surfaced when <see cref="ProcessingStatus"/> is
    /// <see cref="ImageProcessingStatus.Failed"/>. Friendly copy only — the
    /// underlying Magick.NET stack trace lands in AuditLog.
    /// </summary>
    public string? ProcessingError { get; set; }

    public Property Property { get; set; } = null!;
}
