using System.ComponentModel.DataAnnotations;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.CRM.Birthday;

public class UpcomingBirthdayDto
{
    public Guid ContactId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public Language PreferredLanguage { get; set; }
    public bool ConsentMarketing { get; set; }

    /// <summary>0 = today, 1 = tomorrow, …</summary>
    public int DaysUntil { get; set; }
}

public class BirthdayTemplateDto
{
    public Guid Id { get; set; }
    public List<BirthdayTemplateTranslationDto> Translations { get; set; } = new();
}

public class BirthdayTemplateTranslationDto
{
    public Language Language { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
}

public class BirthdayTemplateUpsertDto
{
    [Required]
    public List<BirthdayTemplateTranslationDto> Translations { get; set; } = new();
}

public class BirthdaySendResultDto
{
    public int Eligible { get; set; }
    public int Sent { get; set; }
    public int Skipped { get; set; }
    public bool AutoSendEnabled { get; set; }
}
