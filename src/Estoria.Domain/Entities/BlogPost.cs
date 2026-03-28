using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class BlogPost : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public Guid AuthorId { get; set; }
    public BlogPostStatus Status { get; set; } = BlogPostStatus.Draft;
    public DateTime? PublishedAt { get; set; }

    public TeamMember Author { get; set; } = null!;
    public List<BlogPostTranslation> Translations { get; set; } = [];
}
