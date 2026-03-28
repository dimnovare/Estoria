using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class PageContentTranslation : BaseEntity
{
    public Guid PageContentId { get; set; }
    public Language Language { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }

    public PageContent PageContent { get; set; } = null!;
}
