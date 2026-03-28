using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Blog;

public class AdminBlogDetailDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public BlogPostStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<Language, BlogTranslationDto> Translations { get; set; } = [];
}
