namespace Estoria.Application.DTOs.Services;

public class ServiceListDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? IconName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PriceInfo { get; set; }
}
