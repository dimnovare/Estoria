using Estoria.Application.DTOs.Team;

namespace Estoria.Application.DTOs.Blog;

public class BlogPostDetailDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public DateTime? PublishedAt { get; set; }
    public TeamMemberListDto Author { get; set; } = null!;
}
