namespace Estoria.Application.DTOs.Mailbox;

/// <summary>
/// Full message shape for the detail view — includes HTML body and the
/// attachment manifest (id + name + size, no content). Attachment bytes
/// stream separately via <c>GetAttachmentAsync</c>.
/// </summary>
public class MailboxMessageDetailDto : MailboxMessageDto
{
    public List<MailboxAddressDto> BccRecipients { get; set; } = [];

    /// <summary>Body HTML when available — Graph emits "html" or "text" content type.</summary>
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }

    public List<MailboxAttachmentDto> Attachments { get; set; } = [];
}

public class MailboxAttachmentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public bool IsInline { get; set; }
}
