using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class SiteSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public SettingValueType ValueType { get; set; }

    /// <summary>
    /// Per-language overrides — populated only for keys in the
    /// <c>SiteSettingService.TranslatableKeys</c> whitelist. Empty list for
    /// the operational majority of settings.
    /// </summary>
    public List<SiteSettingTranslation> Translations { get; set; } = [];
}
