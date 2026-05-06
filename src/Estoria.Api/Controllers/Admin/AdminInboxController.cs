using Estoria.Application.DTOs.Mailbox;
using Estoria.Application.Interfaces;
using Estoria.Application.Services;
using Estoria.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Api.Controllers.Admin;

/// <summary>
/// Admin-only proxy in front of Microsoft Graph for the info@ shared mailbox.
/// The browser never sees Graph tokens — every call here resolves to a
/// server-side Graph request. Reads are open to Agent + Admin; mutations
/// (send, link, archive, mark-read) are gated by [Authorize(Roles = ...)]
/// at the action level.
/// </summary>
[ApiController]
[Route("api/admin/inbox")]
[Authorize(Roles = "Admin,Agent")]
public class AdminInboxController : ControllerBase
{
    private const int MaxAttachmentBytes = 10 * 1024 * 1024;

    private readonly IMailboxService _mailbox;
    private readonly MailboxLinkService _links;
    private readonly IAppDbContext _db;
    private readonly AuditService _audit;

    public AdminInboxController(
        IMailboxService mailbox,
        MailboxLinkService links,
        IAppDbContext db,
        AuditService audit)
    {
        _mailbox = mailbox;
        _links   = links;
        _db      = db;
        _audit   = audit;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Reads
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet("counts")]
    public async Task<IActionResult> GetCounts(CancellationToken ct = default)
    {
        var counts = await _links.GetFolderCountsAsync(ct);
        return Ok(counts);
    }

    [HttpGet("messages")]
    public async Task<IActionResult> List(
        [FromQuery] int top              = 50,
        [FromQuery] string? skipToken    = null,
        [FromQuery] bool unreadOnly      = false,
        CancellationToken ct             = default)
    {
        top = Math.Clamp(top, 1, 100);

        var page = await _mailbox.ListInboxAsync(top, skipToken, unreadOnly, ct);

        // Enrich with whatever links we already have so the grid can show
        // "linked to <contact>". One query, IN clause across the page.
        var ids = page.Items.Select(i => i.Id).Where(s => !string.IsNullOrEmpty(s)).ToList();
        var links = ids.Count == 0
            ? new List<InboxLinkProjection>()
            : await _db.MailboxLinks
                .AsNoTracking()
                .Where(l => ids.Contains(l.GraphMessageId))
                .Select(l => new InboxLinkProjection
                {
                    GraphMessageId = l.GraphMessageId,
                    ContactId      = l.ContactId,
                    ContactName    = l.Contact != null ? l.Contact.FullName : null,
                    DealId         = l.DealId,
                    DealTitle      = l.Deal != null ? l.Deal.Title : null,
                    PropertyId     = l.PropertyId,
                    IsArchived     = l.IsArchived,
                })
                .ToListAsync(ct);

        var byId = links.ToDictionary(l => l.GraphMessageId);

        // Flat shape — frontend's InboxMessageSummary expects a single-level
        // object per row. fromName has a defence-in-depth fallback so the
        // frontend's initials() helper can never see null.
        return Ok(new
        {
            items = page.Items.Select(m =>
            {
                byId.TryGetValue(m.Id, out var l);
                return new
                {
                    id              = m.Id,
                    folder          = "Inbox",
                    from            = m.From?.Address ?? string.Empty,
                    fromName        = !string.IsNullOrWhiteSpace(m.From?.Name)
                                        ? m.From!.Name
                                        : !string.IsNullOrWhiteSpace(m.From?.Address)
                                            ? m.From!.Address
                                            : "(unknown sender)",
                    to              = m.ToRecipients.Select(t => t.Address).ToArray(),
                    subject         = m.Subject,
                    preview         = m.BodyPreview,
                    receivedAt      = m.ReceivedAt,
                    isRead          = m.IsRead,
                    hasAttachments  = m.HasAttachments,
                    linkedContactId   = l?.ContactId,
                    linkedContactName = l?.ContactName,
                    linkedDealId      = l?.DealId,
                    linkedDealTitle   = l?.DealTitle,
                    linkedPropertyId  = l?.PropertyId,
                };
            }).ToList(),
            nextSkipToken = page.NextSkipToken,
        });
    }

    [HttpGet("messages/{messageId}")]
    public async Task<IActionResult> Get(string messageId, CancellationToken ct = default)
    {
        var msg = await _mailbox.GetMessageAsync(messageId, ct);
        if (msg is null) return NotFound();

        var link = await _db.MailboxLinks
            .AsNoTracking()
            .Where(l => l.GraphMessageId == messageId)
            .Select(l => new
            {
                l.ContactId, l.DealId, l.PropertyId, l.IsArchived,
                contactName = l.Contact != null ? l.Contact.FullName : null,
                dealTitle   = l.Deal != null    ? l.Deal.Title       : null,
            })
            .FirstOrDefaultAsync(ct);

        return Ok(new
        {
            id              = msg.Id,
            folder          = "Inbox",
            from            = msg.From?.Address ?? string.Empty,
            fromName        = !string.IsNullOrWhiteSpace(msg.From?.Name)
                                ? msg.From!.Name
                                : !string.IsNullOrWhiteSpace(msg.From?.Address)
                                    ? msg.From!.Address
                                    : "(unknown sender)",
            to              = msg.ToRecipients.Select(t => t.Address).ToArray(),
            cc              = msg.CcRecipients.Select(t => t.Address).ToArray(),
            bcc             = msg.BccRecipients.Select(t => t.Address).ToArray(),
            subject         = msg.Subject,
            preview         = msg.BodyPreview,
            bodyHtml        = msg.BodyHtml ?? msg.BodyText ?? string.Empty,
            receivedAt      = msg.ReceivedAt,
            isRead          = msg.IsRead,
            hasAttachments  = msg.HasAttachments,
            attachments     = msg.Attachments.Select(a => new
            {
                id          = a.Id,
                name        = a.Name,
                contentType = a.ContentType,
                size        = a.Size,
                isInline    = a.IsInline,
            }).ToArray(),
            linkedContactId   = link?.ContactId,
            linkedContactName = link?.contactName,
            linkedDealId      = link?.DealId,
            linkedDealTitle   = link?.dealTitle,
            linkedPropertyId  = link?.PropertyId,
            isArchived        = link?.IsArchived ?? false,
        });
    }

    [HttpGet("messages/{messageId}/attachments/{attachmentId}")]
    public async Task<IActionResult> GetAttachment(
        string messageId,
        string attachmentId,
        CancellationToken ct = default)
    {
        var stream = await _mailbox.GetAttachmentAsync(messageId, attachmentId, ct);
        if (stream is null) return NotFound();

        // FileStreamResult disposes the underlying stream automatically.
        return File(stream.Content, stream.ContentType, stream.FileName);
    }

    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetConversation(string conversationId, CancellationToken ct = default)
    {
        var page = await _mailbox.ListConversationAsync(conversationId, ct);
        return Ok(page);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Mutations
    // ─────────────────────────────────────────────────────────────────────

    [HttpPost("messages/{messageId}/read")]
    public async Task<IActionResult> MarkRead(
        string messageId,
        [FromBody] MarkReadRequest body,
        CancellationToken ct = default)
    {
        await _mailbox.MarkReadAsync(messageId, body.IsRead, ct);
        // Keep the local link row in lock-step so the sidebar Unread count
        // updates immediately, without waiting for the next delta tick.
        await _links.MirrorReadStatusAsync(messageId, body.IsRead, ct);
        return NoContent();
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send(
        [FromBody] SendMailboxRequest req,
        CancellationToken ct = default)
    {
        if (req.Message is null || req.Message.To.Count == 0)
            return BadRequest(new { error = "At least one recipient is required." });

        // Reject oversized attachments early — Graph caps single-message
        // payloads around 4 MB without resorting to the upload-session API,
        // and our Kestrel limit is 10 MB; pick the smaller bound here.
        var totalBytes = req.Message.Attachments.Sum(a => a.ContentBase64.Length * 3L / 4);
        if (totalBytes > MaxAttachmentBytes)
            return BadRequest(new { error = "Attachments exceed the 10 MB size limit." });

        // Language fallback: if the controller wasn't told and the linked
        // Contact has a PreferredLanguage, propagate that into the DTO so
        // downstream templating (if any) picks it up.
        if (req.Message.Language is null && req.ContactId is { } cid)
        {
            req.Message.Language = await _db.Contacts
                .Where(c => c.Id == cid)
                .Select(c => (Estoria.Domain.Enums.Language?)c.PreferredLanguage)
                .FirstOrDefaultAsync(ct);
        }

        string? internetMessageId;
        try
        {
            internetMessageId = await _mailbox.SendAsync(req.Message, ct);
        }
        catch (Exception ex)
        {
            await _audit.LogAsync(
                "Mailbox.SendFailed",
                entityType: nameof(MailboxLink),
                details: new
                {
                    req.Message.Subject,
                    Recipients = req.Message.To,
                    error = ex.Message,
                },
                ct: ct);
            throw;
        }

        await _links.RecordOutboundAsync(
            internetMessageId,
            req.Message.Subject,
            req.Message.To,
            req.ContactId,
            req.DealId,
            req.PropertyId,
            ct);

        await _audit.LogAsync(
            "Mailbox.Send",
            entityType: nameof(MailboxLink),
            details: new
            {
                req.Message.Subject,
                Recipients = req.Message.To,
                CcCount    = req.Message.Cc.Count,
                BccCount   = req.Message.Bcc.Count,
                AttachmentCount = req.Message.Attachments.Count,
                internetMessageId,
                req.ContactId,
                req.DealId,
            },
            ct: ct);

        return Ok(new { internetMessageId });
    }

    [HttpPost("messages/{messageId}/link")]
    public async Task<IActionResult> Link(
        string messageId,
        [FromBody] LinkRequest body,
        CancellationToken ct = default)
    {
        var link = await _links.LinkAsync(
            messageId,
            body.ContactId,
            body.DealId,
            body.PropertyId,
            ct);

        await _audit.LogAsync(
            "Mailbox.Link",
            entityType: nameof(MailboxLink),
            entityId: link.Id,
            details: new { body.ContactId, body.DealId, body.PropertyId },
            ct: ct);

        return Ok(new { link.Id, link.ContactId, link.DealId, link.PropertyId });
    }

    [HttpPost("messages/{messageId}/archive")]
    public async Task<IActionResult> Archive(string messageId, CancellationToken ct = default)
    {
        var ok = await _links.ArchiveAsync(messageId, ct);
        if (!ok) return NotFound();

        await _audit.LogAsync(
            "Mailbox.Archive",
            entityType: nameof(MailboxLink),
            details: new { messageId },
            ct: ct);

        return NoContent();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Request shapes
    // ─────────────────────────────────────────────────────────────────────

    public class MarkReadRequest
    {
        public bool IsRead { get; set; }
    }

    public class SendMailboxRequest
    {
        public SendMailboxMessageDto Message { get; set; } = new();
        public Guid? ContactId { get; set; }
        public Guid? DealId { get; set; }
        public Guid? PropertyId { get; set; }
    }

    public class LinkRequest
    {
        public Guid? ContactId { get; set; }
        public Guid? DealId { get; set; }
        public Guid? PropertyId { get; set; }
    }

    private class InboxLinkProjection
    {
        public string GraphMessageId { get; set; } = string.Empty;
        public Guid? ContactId { get; set; }
        public string? ContactName { get; set; }
        public Guid? DealId { get; set; }
        public string? DealTitle { get; set; }
        public Guid? PropertyId { get; set; }
        public bool IsArchived { get; set; }
    }
}
