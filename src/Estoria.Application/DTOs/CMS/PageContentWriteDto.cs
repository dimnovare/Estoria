using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.CMS;

public class PageTranslationDto
{
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
}

public class UpdatePageContentDto
{
    public Dictionary<Language, PageTranslationDto> Translations { get; set; } = [];
}
