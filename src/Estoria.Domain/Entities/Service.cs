using Estoria.Domain.Base;

namespace Estoria.Domain.Entities;

public class Service : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string? IconName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public List<ServiceTranslation> Translations { get; set; } = [];
}
