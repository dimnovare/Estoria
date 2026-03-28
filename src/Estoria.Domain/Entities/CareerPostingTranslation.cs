using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class CareerPostingTranslation : BaseEntity
{
    public Guid CareerPostingId { get; set; }
    public Language Language { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }

    public CareerPosting CareerPosting { get; set; } = null!;
}
