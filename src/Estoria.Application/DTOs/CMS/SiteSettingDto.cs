using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.CMS;

public class SiteSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public SettingValueType ValueType { get; set; }
}

public class UpdateSiteSettingDto
{
    public string? Value { get; set; }
}

/// <summary>
/// Admin-edit shape. <see cref="IsTranslatable"/> tells the UI whether to
/// render language tabs; <see cref="Translations"/> is empty for the
/// operational majority of keys (toggles, URLs, numeric stats).
/// </summary>
public class SiteSettingAdminDetailDto
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public SettingValueType ValueType { get; set; }
    public bool IsTranslatable { get; set; }
    public List<SiteSettingTranslationDto> Translations { get; set; } = [];
}

public class SiteSettingTranslationDto
{
    public Language Language { get; set; }
    public string Value { get; set; } = string.Empty;
}
