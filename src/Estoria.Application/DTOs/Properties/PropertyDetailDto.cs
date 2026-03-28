using Estoria.Application.DTOs.Team;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Properties;

public class PropertyDetailDto
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
    public bool IsFeatured { get; set; }

    // Detail-only fields
    public string Description { get; set; } = string.Empty;
    public string? District { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? Floor { get; set; }
    public int? TotalFloors { get; set; }
    public int? YearBuilt { get; set; }
    public string? EnergyClass { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<string> Features { get; set; } = [];
    public List<PropertyImageDto> Images { get; set; } = [];
    public TeamMemberListDto Agent { get; set; } = null!;
    public DateTime? PublishedAt { get; set; }
}
