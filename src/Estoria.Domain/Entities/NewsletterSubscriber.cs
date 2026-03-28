using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class NewsletterSubscriber : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public Language Language { get; set; } = Language.En;
    public bool IsActive { get; set; } = true;
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
}
