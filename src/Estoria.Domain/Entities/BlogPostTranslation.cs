using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class BlogPostTranslation : BaseEntity
{
    public Guid BlogPostId { get; set; }
    public Language Language { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public BlogPost BlogPost { get; set; } = null!;
}
