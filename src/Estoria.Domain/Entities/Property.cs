using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class Property : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
    public PropertyType PropertyType { get; set; }
    public PropertyStatus Status { get; set; } = PropertyStatus.Draft;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal Size { get; set; }
    public int? Rooms { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? Floor { get; set; }
    public int? TotalFloors { get; set; }
    public int? YearBuilt { get; set; }
    public string? EnergyClass { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsFeatured { get; set; }
    public Guid AgentId { get; set; }
    public DateTime? PublishedAt { get; set; }

    public TeamMember Agent { get; set; } = null!;
    public List<PropertyTranslation> Translations { get; set; } = [];
    public List<PropertyImage> Images { get; set; } = [];
    public List<PropertyFeature> Features { get; set; } = [];
}
