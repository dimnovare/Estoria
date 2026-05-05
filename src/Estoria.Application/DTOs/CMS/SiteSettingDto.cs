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
