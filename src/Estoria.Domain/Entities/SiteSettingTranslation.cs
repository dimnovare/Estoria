using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

/// <summary>
/// Per-language override for a SiteSetting. Only keys in the
/// <c>SiteSettingService.TranslatableKeys</c> whitelist participate — most
/// settings (toggles, URLs, numeric stats) don't need translation, so they
/// stay as a single Value on the parent row.
/// </summary>
public class SiteSettingTranslation : BaseEntity
{
    public Guid SiteSettingId { get; set; }
    public Language Language { get; set; }
    public string Value { get; set; } = string.Empty;

    public SiteSetting SiteSetting { get; set; } = null!;
}
