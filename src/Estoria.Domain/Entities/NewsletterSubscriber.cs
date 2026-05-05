using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class NewsletterSubscriber : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public Language Language { get; set; } = Language.En;
    public bool IsActive { get; set; } = true;
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Random opaque token for the public unsubscribe URL. 64 chars max.
    /// Generated fresh on subscribe; backfilled for legacy rows in the
    /// AddNewsletterUnsubscribeToken migration.
    /// </summary>
    public string UnsubscribeToken { get; set; } = string.Empty;
}
