namespace Estoria.Application.DTOs.Public;

public class PropertyTypeOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class PropertyTypeOptionsDto
{
    public List<PropertyTypeOptionDto> PropertyTypes { get; set; } = [];
    public List<PropertyTypeOptionDto> TransactionTypes { get; set; } = [];
}
