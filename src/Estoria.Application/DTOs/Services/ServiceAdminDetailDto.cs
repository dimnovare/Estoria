namespace Estoria.Application.DTOs.Services;

/// <summary>
/// Admin-edit shape for an offered service. Includes every translation
/// (PascalCase-keyed) so the form populates all language tabs from one
/// fetch. PriceInfo is per-translation and lives inside the dict.
/// </summary>
public class ServiceAdminDetailDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? IconName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Dictionary<string, ServiceTranslationDto> Translations { get; set; } = new();
}
