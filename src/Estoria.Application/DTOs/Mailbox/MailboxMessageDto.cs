namespace Estoria.Application.DTOs.Mailbox;

/// <summary>
/// Lightweight inbox-list shape — no body, no attachment bytes. Maps the
/// subset of Graph Message fields we expose to the admin inbox grid.
/// </summary>
public class MailboxMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string InternetMessageId { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;

    public MailboxAddressDto? From { get; set; }
    public List<MailboxAddressDto> ToRecipients { get; set; } = [];
    public List<MailboxAddressDto> CcRecipients { get; set; } = [];

    public DateTime? SentAt { get; set; }
    public DateTime? ReceivedAt { get; set; }

    public bool HasAttachments { get; set; }
    public bool IsRead { get; set; }
}

public class MailboxAddressDto
{
    public string? Name { get; set; }
    public string Address { get; set; } = string.Empty;
}
