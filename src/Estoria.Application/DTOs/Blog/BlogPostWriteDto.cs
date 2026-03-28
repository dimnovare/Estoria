using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Blog;

public class BlogTranslationDto
{
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class CreateBlogPostDto
{
    public Guid AuthorId { get; set; }
    public string? CoverImageUrl { get; set; }
    public Dictionary<Language, BlogTranslationDto> Translations { get; set; } = [];
}

public class UpdateBlogPostDto : CreateBlogPostDto { }
