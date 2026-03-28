using Estoria.Domain.Base;

namespace Estoria.Domain.Entities;

public class PropertyImage : BaseEntity
{
    public Guid PropertyId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }

    public Property Property { get; set; } = null!;
}
