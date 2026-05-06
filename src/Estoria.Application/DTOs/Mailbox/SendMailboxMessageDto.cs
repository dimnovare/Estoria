using System.ComponentModel.DataAnnotations;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Mailbox;

/// <summary>
/// Outbound message shape. Maps to Graph's sendMail action — the API layer
/// validates recipient lists and clamps attachment sizes before this hits
/// the GraphMailService.
/// </summary>
public class SendMailboxMessageDto
{
    [Required, MinLength(1)]
    public List<string> To { get; set; } = [];

    public List<string> Cc { get; set; } = [];
    public List<string> Bcc { get; set; } = [];

    [Required, MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string BodyHtml { get; set; } = string.Empty;

    public List<SendMailboxAttachmentDto> Attachments { get; set; } = [];

    /// <summary>
    /// When set, the outbound message threads as a reply — Graph keeps the
    /// In-Reply-To / References headers correct via createReply or the
    /// internetMessageId on a fresh message.
    /// </summary>
    public string? InReplyToInternetMessageId { get; set; }

    /// <summary>
    /// Optional language hint for templated content. Falls back to the linked
    /// contact's preferred language when omitted at the controller layer.
    /// </summary>
    public Language? Language { get; set; }
}

public class SendMailboxAttachmentDto
{
    [Required, MaxLength(255)]
    public string Filename { get; set; } = string.Empty;

    /// <summary>Base64-encoded bytes — same shape as Graph fileAttachment.contentBytes.</summary>
    [Required]
    public string ContentBase64 { get; set; } = string.Empty;

    public string? ContentType { get; set; }
}
