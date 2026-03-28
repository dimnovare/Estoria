using Estoria.Domain.Base;

namespace Estoria.Domain.Entities;

public class PropertyFeature : BaseEntity
{
    public Guid PropertyId { get; set; }
    public string Feature { get; set; } = string.Empty;

    public Property Property { get; set; } = null!;
}
