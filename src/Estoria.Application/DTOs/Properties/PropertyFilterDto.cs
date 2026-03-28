using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Properties;

public class PropertyFilterDto
{
    public PropertyType? Type { get; set; }
    public TransactionType? Transaction { get; set; }
    public string? City { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinSize { get; set; }
    public decimal? MaxSize { get; set; }
}
