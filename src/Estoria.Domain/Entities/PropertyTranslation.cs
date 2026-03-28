using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class PropertyTranslation : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Language Language { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }

    public Property Property { get; set; } = null!;
}
