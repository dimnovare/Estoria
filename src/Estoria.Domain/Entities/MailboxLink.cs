using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

/// <summary>
/// Bridges a Graph message to our CRM. One row per processed message;
/// linking lets the inbox UI show "this email is from Andrei about Deal X"
/// and lets contact/deal pages list the related correspondence without
/// hitting Graph for every render.
/// </summary>
public class MailboxLink : BaseEntity
{
    /// <summary>Graph message id — opaque, mailbox-scoped.</summary>
    public string GraphMessageId { get; set; } = string.Empty;

    /// <summary>Graph conversationId — groups all messages on a thread.</summary>
    public string GraphConversationId { get; set; } = string.Empty;

    /// <summary>RFC 2822 Message-Id header — stable across mailboxes.</summary>
    public string InternetMessageId { get; set; } = string.Empty;

    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? PropertyId { get; set; }

    public MailDirection Direction { get; set; } = MailDirection.Inbound;

    public string Subject { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }

    public bool IsArchived { get; set; }

    public Contact? Contact { get; set; }
    public Deal? Deal { get; set; }
    public Property? Property { get; set; }
}
