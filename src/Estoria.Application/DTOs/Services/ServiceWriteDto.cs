using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Services;

public class ServiceTranslationDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PriceInfo { get; set; }
}

public class CreateServiceDto
{
    public string? IconName { get; set; }
    public int SortOrder { get; set; }
    public Dictionary<Language, ServiceTranslationDto> Translations { get; set; } = [];
}

public class UpdateServiceDto : CreateServiceDto { }
