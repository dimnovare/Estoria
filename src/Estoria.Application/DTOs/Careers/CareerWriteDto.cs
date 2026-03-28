using System.ComponentModel.DataAnnotations;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Careers;

public class CareerTranslationDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public class CreateCareerDto
{
    public Dictionary<Language, CareerTranslationDto> Translations { get; set; } = [];
}

public class UpdateCareerDto : CreateCareerDto { }
