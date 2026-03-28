using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class ServiceTranslation : BaseEntity
{
    public Guid ServiceId { get; set; }
    public Language Language { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PriceInfo { get; set; }

    public Service Service { get; set; } = null!;
}
