namespace Estoria.Application.DTOs.Blog;

public class BlogPostListDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? CoverImageUrl { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorPhotoUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
}
