using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

/// <summary>
/// One outbound newsletter "blast". Captures the rendered subject + body that
/// went out (or is going out), the language scope, and the per-run tally so
/// the admin list view can show "23/25 delivered" without re-querying Resend.
/// </summary>
public class NewsletterCampaign : BaseEntity
{
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;

    /// <summary>Null = send to every active subscriber regardless of language.</summary>
    public Language? LanguageFilter { get; set; }

    public NewsletterCampaignStatus Status { get; set; } = NewsletterCampaignStatus.Draft;

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int RecipientsCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }

    public Guid SentByUserId { get; set; }
}
