using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.CMS;

public class AdminPageContentDto
{
    public Guid Id { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public Dictionary<Language, PageTranslationDto> Translations { get; set; } = [];
}
