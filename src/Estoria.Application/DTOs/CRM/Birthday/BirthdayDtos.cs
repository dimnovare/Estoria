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

    /// <summary>Age the contact will be on their next birthday. Computed server-side.</summary>
    public int TurningAge { get; set; }

    /// <summary>The actual upcoming birthday date (this year or next).</summary>
    public DateOnly NextBirthday { get; set; }
}

public class BirthdayTemplateTranslationDto
{
    public Language Language { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
}

/// <summary>Optional body for POST /send-now — when ContactId is set, only that contact is mailed.</summary>
public class BirthdaySendRequestDto
{
    public Guid? ContactId { get; set; }
}

public class BirthdaySendResultDto
{
    public int Eligible { get; set; }
    public int Sent { get; set; }
    public int Skipped { get; set; }
    public bool AutoSendEnabled { get; set; }
}
