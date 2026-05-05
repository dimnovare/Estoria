using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Properties;

public class PropertyListDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal Size { get; set; }
    public int? Rooms { get; set; }
    public PropertyType PropertyType { get; set; }
    public TransactionType TransactionType { get; set; }
    public PropertyStatus Status { get; set; }
    public string? CoverImageUrl { get; set; }

    /// <summary>Cover variant URLs for srcset rendering — null on legacy rows still on a single Url.</summary>
    public string? CoverThumbUrl { get; set; }
    public string? CoverMediumUrl { get; set; }
    public string? CoverLargeUrl { get; set; }

    public bool IsFeatured { get; set; }
}
