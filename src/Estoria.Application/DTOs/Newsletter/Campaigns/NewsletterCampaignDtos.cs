using System.ComponentModel.DataAnnotations;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Newsletter.Campaigns;

public class NewsletterCampaignDto
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public Language? LanguageFilter { get; set; }
    public NewsletterCampaignStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RecipientsCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public Guid SentByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NewsletterCampaignSendDto
{
    [Required, MinLength(3), MaxLength(300)]
    public string Subject { get; set; } = string.Empty;

    [Required, MinLength(10)]
    public string BodyHtml { get; set; } = string.Empty;

    /// <summary>Null = every active subscriber regardless of preferred language.</summary>
    public Language? Language { get; set; }
}
