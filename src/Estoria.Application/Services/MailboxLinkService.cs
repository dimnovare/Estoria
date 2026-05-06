using Estoria.Application.DTOs.Inbox;
using Estoria.Application.DTOs.Mailbox;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Estoria.Application.Services;

/// <summary>
/// Bridges Graph messages and the CRM. Owns the sync job (Hangfire), the
/// upsert/auto-match path, and the manual link-override surface used by the
/// admin UI when the heuristic miss-matches.
///
/// The sync job is delta-based: the cursor lives in SiteSettings under
/// <c>graph.inbox_delta_link</c> so re-runs only fetch what's changed since
/// the previous tick. First run (no cursor) pulls a recent slice.
/// </summary>
public class MailboxLinkService
{
    private const string DeltaSettingKey = "graph.inbox_delta_link";

    private readonly IAppDbContext _db;
    private readonly IMailboxService _mailbox;
    private readonly SiteSettingService _settings;
    private readonly ILogger<MailboxLinkService> _logger;

    public MailboxLinkService(
        IAppDbContext db,
        IMailboxService mailbox,
        SiteSettingService settings,
        ILogger<MailboxLinkService> logger)
    {
        _db       = db;
        _mailbox  = mailbox;
        _settings = settings;
        _logger   = logger;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Hangfire entry point
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Delta-pulls the inbox and upserts MailboxLink rows for new messages.
    /// Idempotent — replaying the same delta page is a no-op thanks to the
    /// unique index on GraphMessageId / InternetMessageId.
    /// </summary>
    public async Task<int> SyncInboxAsync(CancellationToken ct = default)
    {
        // Read the persisted cursor; if absent, the underlying service does
        // a fresh delta pull. The cursor flows back from Graph as
        // OdataDeltaLink — we don't generate it ourselves.
        var page = await _mailbox.SyncInboxDeltaAsync(ct);

        var linked = 0;
        foreach (var msg in page.Items)
        {
            try
            {
                var inserted = await UpsertInboundLinkAsync(msg, ct);
                if (inserted) linked++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "MAILBOX_SYNC_UPSERT_FAIL graphMessageId={Id}", msg.Id);
            }
        }

        if (!string.IsNullOrEmpty(page.NextDeltaLink))
        {
            await _settings.UpsertAsync(
                DeltaSettingKey,
                page.NextDeltaLink,
                SettingValueType.Text,
                ct);
        }

        _logger.LogInformation(
            "MAILBOX_SYNC fetched={Fetched} linked={Linked} hasCursor={Cursor}",
            page.Items.Count, linked, !string.IsNullOrEmpty(page.NextDeltaLink));

        return linked;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Upsert + auto-match
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inserts a MailboxLink for an inbound message if one doesn't already
    /// exist, attempts a sender → Contact match, and emits an Email Activity
    /// when the match succeeds and the contact has an assigned agent (or a
    /// fallback Admin user is available).
    /// Returns true when a new link was created.
    /// </summary>
    public async Task<bool> UpsertInboundLinkAsync(
        MailboxMessageDto msg,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(msg.Id) || string.IsNullOrEmpty(msg.InternetMessageId))
            return false;

        var existing = await _db.MailboxLinks
            .FirstOrDefaultAsync(l => l.GraphMessageId == msg.Id, ct);
        if (existing is not null)
        {
            // Re-syncing an already-known message: keep IsRead in step with
            // Graph (someone may have read it from Outlook between ticks) so
            // the sidebar Unread count reflects reality.
            if (existing.IsRead != msg.IsRead)
            {
                existing.IsRead = msg.IsRead;
                await _db.SaveChangesAsync(ct);
            }
            return false;
        }

        var fromEmail = msg.From?.Address?.Trim() ?? string.Empty;

        // Sender match — case-insensitive, exact only. We don't fuzzy-match
        // (e.g. partial domain) because false positives here cross-contaminate
        // the activity timeline of unrelated contacts.
        Guid? matchedContactId = null;
        if (!string.IsNullOrEmpty(fromEmail))
        {
            var lower = fromEmail.ToLower();
            matchedContactId = await _db.Contacts
                .Where(c => c.Email != null && c.Email.ToLower() == lower)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(ct);
        }

        var link = new MailboxLink
        {
            GraphMessageId      = msg.Id,
            GraphConversationId = msg.ConversationId,
            InternetMessageId   = msg.InternetMessageId,
            ContactId           = matchedContactId,
            Direction           = MailDirection.Inbound,
            Subject             = Truncate(msg.Subject, 500),
            FromAddress         = Truncate(fromEmail, 320),
            ReceivedAt          = msg.ReceivedAt ?? DateTime.UtcNow,
            IsRead              = msg.IsRead,
        };

        _db.MailboxLinks.Add(link);
        await _db.SaveChangesAsync(ct);

        if (matchedContactId.HasValue)
            await TryWriteEmailActivityAsync(msg, matchedContactId.Value, ct);

        return true;
    }

    private async Task TryWriteEmailActivityAsync(
        MailboxMessageDto msg,
        Guid contactId,
        CancellationToken ct)
    {
        // Activity.UserId is required, so we need a "performer". Order:
        // 1) the contact's AssignedAgent (best semantics — that's whose
        //    inbox this maps to in the agent's view),
        // 2) any Admin user (system-fallback so the activity still lands).
        // No suitable user → log a WARN and skip; the link still exists, so
        // the inbox UI shows the message — only the timeline entry is missed.
        var contact = await _db.Contacts
            .Where(c => c.Id == contactId)
            .Select(c => new { c.AssignedAgentId })
            .FirstOrDefaultAsync(ct);

        Guid? userId = contact?.AssignedAgentId;
        if (userId is null)
        {
            userId = await _db.Users
                .Where(u => u.IsActive)
                .Join(_db.UserRoles,
                    u => u.Id,
                    r => r.UserId,
                    (u, r) => new { u.Id, r.Role })
                .Where(x => x.Role == UserRole.Admin)
                .OrderBy(x => x.Id)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(ct);
        }

        if (userId is null)
        {
            _logger.LogWarning(
                "MAILBOX_NO_USER_FOR_ACTIVITY contactId={ContactId} graphMessageId={MsgId}",
                contactId, msg.Id);
            return;
        }

        _db.Activities.Add(new Activity
        {
            ContactId  = contactId,
            UserId     = userId.Value,
            Type       = ActivityType.Email,
            Title      = Truncate(msg.Subject, 300),
            Body       = msg.BodyPreview,
            OccurredAt = msg.ReceivedAt ?? DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Manual overrides
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Manual link override. Used when the auto-match misses (e.g. message
    /// from a personal address that hasn't been added as a Contact yet, or
    /// a forwarded thread the agent wants pinned to a specific deal).
    /// Creates the link from a Graph fetch if it doesn't exist yet.
    /// </summary>
    public async Task<MailboxLink> LinkAsync(
        string graphMessageId,
        Guid? contactId,
        Guid? dealId,
        Guid? propertyId,
        CancellationToken ct = default)
    {
        var link = await _db.MailboxLinks
            .FirstOrDefaultAsync(l => l.GraphMessageId == graphMessageId, ct);

        if (link is null)
        {
            // Pull the message just enough to populate the metadata fields.
            var msg = await _mailbox.GetMessageAsync(graphMessageId, ct)
                ?? throw new KeyNotFoundException($"Graph message {graphMessageId} not found.");

            link = new MailboxLink
            {
                GraphMessageId      = msg.Id,
                GraphConversationId = msg.ConversationId,
                InternetMessageId   = msg.InternetMessageId,
                Direction           = MailDirection.Inbound,
                Subject             = Truncate(msg.Subject, 500),
                FromAddress         = Truncate(msg.From?.Address ?? string.Empty, 320),
                ReceivedAt          = msg.ReceivedAt ?? DateTime.UtcNow,
            };
            _db.MailboxLinks.Add(link);
        }

        // Null on a parameter clears that link; passing a value sets it.
        // The controller decides whether the caller intended a clear by
        // sending the field explicitly.
        link.ContactId  = contactId;
        link.DealId     = dealId;
        link.PropertyId = propertyId;

        await _db.SaveChangesAsync(ct);
        return link;
    }

    public async Task<bool> ArchiveAsync(string graphMessageId, CancellationToken ct = default)
    {
        var link = await _db.MailboxLinks
            .FirstOrDefaultAsync(l => l.GraphMessageId == graphMessageId, ct);
        if (link is null) return false;

        link.IsArchived = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Mirrors a Graph MarkRead call into the local link row so the inbox
    /// sidebar's Unread count stays correct without waiting for the next
    /// delta sync.
    /// </summary>
    public async Task MirrorReadStatusAsync(
        string graphMessageId,
        bool isRead,
        CancellationToken ct = default)
    {
        var link = await _db.MailboxLinks
            .FirstOrDefaultAsync(l => l.GraphMessageId == graphMessageId, ct);
        if (link is null) return;

        if (link.IsRead == isRead) return;
        link.IsRead = isRead;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Folder counts for the inbox sidebar. Backed by MailboxLink, not Graph
    /// — the delta sync keeps the table fresh, and this stays cheap to call
    /// on every page load.
    /// </summary>
    public async Task<InboxFolderCountsDto> GetFolderCountsAsync(CancellationToken ct = default)
    {
        var inbox = await _db.MailboxLinks
            .CountAsync(m => !m.IsArchived && m.Direction == MailDirection.Inbound, ct);
        var unread = await _db.MailboxLinks
            .CountAsync(m => !m.IsArchived && m.Direction == MailDirection.Inbound && !m.IsRead, ct);
        var sent = await _db.MailboxLinks
            .CountAsync(m => m.Direction == MailDirection.Outbound, ct);
        var archive = await _db.MailboxLinks
            .CountAsync(m => m.IsArchived, ct);

        return new InboxFolderCountsDto
        {
            Inbox   = inbox,
            Unread  = unread,
            Sent    = sent,
            Archive = archive,
        };
    }

    /// <summary>
    /// Records an outbound message we just sent. Distinct from inbound
    /// upsert because we already know what entities the agent linked the
    /// reply against (the controller passes them through).
    /// </summary>
    public async Task RecordOutboundAsync(
        string? internetMessageId,
        string subject,
        IReadOnlyList<string> recipients,
        Guid? contactId,
        Guid? dealId,
        Guid? propertyId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(internetMessageId)) return;

        var existing = await _db.MailboxLinks
            .FirstOrDefaultAsync(l => l.InternetMessageId == internetMessageId, ct);
        if (existing is not null) return;

        _db.MailboxLinks.Add(new MailboxLink
        {
            GraphMessageId      = internetMessageId,
            GraphConversationId = string.Empty,
            InternetMessageId   = internetMessageId,
            ContactId           = contactId,
            DealId              = dealId,
            PropertyId          = propertyId,
            Direction           = MailDirection.Outbound,
            Subject             = Truncate(subject, 500),
            FromAddress         = string.Empty,
            ReceivedAt          = DateTime.UtcNow,
            IsRead              = true,
        });

        await _db.SaveChangesAsync(ct);
    }

    private static string Truncate(string value, int max)
        => string.IsNullOrEmpty(value) ? value : (value.Length > max ? value[..max] : value);
}
