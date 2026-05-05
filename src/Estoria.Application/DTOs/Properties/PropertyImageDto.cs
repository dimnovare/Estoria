using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Properties;

public class PropertyImageDto
{
    public Guid Id { get; set; }

    /// <summary>Legacy "best available" URL — populated from MediumUrl after processing.</summary>
    public string Url { get; set; } = string.Empty;

    public string? ThumbUrl { get; set; }
    public string? MediumUrl { get; set; }
    public string? LargeUrl { get; set; }

    public ImageProcessingStatus ProcessingStatus { get; set; } = ImageProcessingStatus.Done;
    public string? ProcessingError { get; set; }

    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
}
