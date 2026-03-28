using Estoria.Domain.Base;

namespace Estoria.Domain.Entities;

public class CareerPosting : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public List<CareerPostingTranslation> Translations { get; set; } = [];
}
