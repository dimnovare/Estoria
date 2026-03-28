namespace Estoria.Application.DTOs.CMS;

public class PageContentDto
{
    public Guid Id { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
}
