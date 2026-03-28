using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Properties;

public class PropertyTranslationDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
}

public class CreatePropertyDto
{
    public TransactionType TransactionType { get; set; }
    public PropertyType PropertyType { get; set; }
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
    public List<string> Features { get; set; } = [];
    public Dictionary<Language, PropertyTranslationDto> Translations { get; set; } = [];
}

public class UpdatePropertyDto : CreatePropertyDto { }
