using System.Net;
using Azure.Identity;
using Estoria.Application.DTOs.Mailbox;
using Estoria.Application.Interfaces;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Estoria.Infrastructure.External;

/// <summary>
/// Microsoft Graph implementation of <see cref="IMailboxService"/>. Uses
/// client-credential (app-only) auth scoped via tenant Application Access
/// Policy to a single shared mailbox — the API process never holds delegated
/// user tokens. Throttling (429/503) is retried once with Retry-After honor;
/// anything beyond that bubbles to the caller.
/// </summary>
public class GraphMailService : IMailboxService
{
    private const int MaxRetries = 2;

    private readonly GraphServiceClient _graph;
    private readonly ILogger<GraphMailService> _logger;
    private readonly string _mailbox;

    public GraphMailService(IConfiguration config, ILogger<GraphMailService> logger)
    {
        _logger = logger;

        var section      = config.GetSection("Graph");
        var tenantId     = section["TenantId"]     ?? throw new InvalidOperationException("Graph:TenantId is not configured.");
        var clientId     = section["ClientId"]     ?? throw new InvalidOperationException("Graph:ClientId is not configured.");
        var clientSecret = section["ClientSecret"] ?? throw new InvalidOperationException("Graph:ClientSecret is not configured.");
        _mailbox         = section["Mailbox"]      ?? throw new InvalidOperationException("Graph:Mailbox is not configured.");

        // Client-credential flow → app-only token. Tenant-level scope must be
        // narrowed by an Application Access Policy that targets the security
        // group containing _mailbox; otherwise we can read the entire tenant.
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        _graph = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Inbox listing
    // ─────────────────────────────────────────────────────────────────────

    public async Task<MailboxPage<MailboxMessageDto>> ListInboxAsync(
        int top = 50,
        string? skipToken = null,
        bool unreadOnly = false,
        CancellationToken ct = default)
    {
        return await WithRetryAsync(async ct2 =>
        {
            var msgs = _graph.Users[_mailbox].MailFolders["Inbox"].Messages;

            // Graph v5 doesn't expose $skiptoken on QueryParameters — follow-up
            // pages use WithUrl(nextLink). We treat the cursor as an opaque
            // string from the caller's perspective (it's the full nextLink).
            var page = !string.IsNullOrEmpty(skipToken)
                ? await msgs.WithUrl(skipToken).GetAsync(cancellationToken: ct2)
                : await msgs.GetAsync(rc =>
                {
                    rc.QueryParameters.Top     = top;
                    rc.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                    rc.QueryParameters.Select  = MessageSelect;
                    if (unreadOnly)
                        rc.QueryParameters.Filter = "isRead eq false";
                }, ct2);

            return new MailboxPage<MailboxMessageDto>
            {
                Items         = (page?.Value ?? new List<Message>()).Select(ToListDto).ToList(),
                NextSkipToken = page?.OdataNextLink,
            };
        }, "ListInbox", ct);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Single message + attachments
    // ─────────────────────────────────────────────────────────────────────

    public async Task<MailboxMessageDetailDto?> GetMessageAsync(
        string messageId,
        CancellationToken ct = default)
    {
        return await WithRetryAsync<MailboxMessageDetailDto?>(async ct2 =>
        {
            var msg = await _graph.Users[_mailbox].Messages[messageId].GetAsync(rc =>
            {
                rc.QueryParameters.Select = MessageDetailSelect;
                rc.QueryParameters.Expand = new[] { "attachments($select=id,name,contentType,size,isInline)" };
            }, ct2);

            return msg is null ? null : ToDetailDto(msg);
        }, "GetMessage", ct);
    }

    public async Task<MailboxAttachmentStream?> GetAttachmentAsync(
        string messageId,
        string attachmentId,
        CancellationToken ct = default)
    {
        return await WithRetryAsync<MailboxAttachmentStream?>(async ct2 =>
        {
            var attachment = await _graph.Users[_mailbox]
                .Messages[messageId]
                .Attachments[attachmentId]
                .GetAsync(cancellationToken: ct2);

            // Only file attachments carry inline bytes — itemAttachment /
            // referenceAttachment require different handling that we don't
            // need yet (forwarded items, OneDrive links).
            if (attachment is not FileAttachment file || file.ContentBytes is null)
                return null;

            var ms = new MemoryStream(file.ContentBytes);
            return new MailboxAttachmentStream
            {
                Content     = ms,
                FileName    = file.Name ?? "attachment",
                ContentType = file.ContentType ?? "application/octet-stream",
                Size        = file.Size,
            };
        }, "GetAttachment", ct);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Send
    // ─────────────────────────────────────────────────────────────────────

    public async Task<string?> SendAsync(
        SendMailboxMessageDto dto,
        CancellationToken ct = default)
    {
        // We create a draft first (which returns id + internetMessageId),
        // then send it. Pure sendMail is fire-and-forget, so this two-step
        // is the only way to capture the message id for our MailboxLink row.
        return await WithRetryAsync<string?>(async ct2 =>
        {
            var draft = new Message
            {
                Subject       = dto.Subject,
                Body          = new ItemBody { ContentType = BodyType.Html, Content = dto.BodyHtml },
                ToRecipients  = dto.To.Select(ToRecipient).ToList(),
                CcRecipients  = dto.Cc.Select(ToRecipient).ToList(),
                BccRecipients = dto.Bcc.Select(ToRecipient).ToList(),
            };

            // In-Reply-To is set via internetMessageHeaders on the draft;
            // Graph then strings References + In-Reply-To together for us.
            if (!string.IsNullOrWhiteSpace(dto.InReplyToInternetMessageId))
            {
                draft.InternetMessageHeaders = new List<InternetMessageHeader>
                {
                    new() { Name = "In-Reply-To", Value = dto.InReplyToInternetMessageId },
                };
            }

            if (dto.Attachments.Count > 0)
            {
                draft.Attachments = dto.Attachments.Select(a => (Attachment)new FileAttachment
                {
                    OdataType     = "#microsoft.graph.fileAttachment",
                    Name          = a.Filename,
                    ContentType   = a.ContentType ?? "application/octet-stream",
                    ContentBytes  = Convert.FromBase64String(a.ContentBase64),
                }).ToList();
            }

            var created = await _graph.Users[_mailbox].Messages.PostAsync(draft, cancellationToken: ct2);
            if (created?.Id is null)
            {
                _logger.LogWarning("GRAPH_SEND_DRAFT_NULL mailbox={Mailbox}", _mailbox);
                return null;
            }

            await _graph.Users[_mailbox].Messages[created.Id].Send.PostAsync(cancellationToken: ct2);

            // Graph populates InternetMessageId at draft creation, so this is
            // safe to return after the send call (the id doesn't change).
            return created.InternetMessageId;
        }, "SendMail", ct);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Mutations + threading
    // ─────────────────────────────────────────────────────────────────────

    public async Task MarkReadAsync(string messageId, bool isRead, CancellationToken ct = default)
    {
        await WithRetryAsync<bool>(async ct2 =>
        {
            await _graph.Users[_mailbox].Messages[messageId]
                .PatchAsync(new Message { IsRead = isRead }, cancellationToken: ct2);
            return true;
        }, "MarkRead", ct);
    }

    public async Task<MailboxPage<MailboxMessageDto>> ListConversationAsync(
        string conversationId,
        CancellationToken ct = default)
    {
        return await WithRetryAsync(async ct2 =>
        {
            // Conversation listing pulls across folders (Inbox + Sent Items)
            // by filtering on conversationId at the user level rather than
            // a single folder.
            var page = await _graph.Users[_mailbox].Messages.GetAsync(rc =>
            {
                rc.QueryParameters.Filter  = $"conversationId eq '{EscapeOData(conversationId)}'";
                rc.QueryParameters.Orderby = new[] { "receivedDateTime asc" };
                rc.QueryParameters.Top     = 100;
                rc.QueryParameters.Select  = MessageSelect;
            }, ct2);

            return new MailboxPage<MailboxMessageDto>
            {
                Items         = (page?.Value ?? new List<Message>()).Select(ToListDto).ToList(),
                NextSkipToken = page?.OdataNextLink,
            };
        }, "ListConversation", ct);
    }

    public async Task<MailboxDeltaPage> SyncInboxDeltaAsync(CancellationToken ct = default)
    {
        // Persistence of the delta link is the caller's job; this method just
        // returns "since last full pull". The first call (no prior cursor)
        // returns up to one page of recent messages plus a cursor; subsequent
        // calls return only the changes since that cursor.
        return await WithRetryAsync(async ct2 =>
        {
            var deltaReq = _graph.Users[_mailbox].MailFolders["Inbox"].Messages.Delta;
            var response = await deltaReq.GetAsDeltaGetResponseAsync(rc =>
            {
                rc.QueryParameters.Select = MessageSelect;
                rc.QueryParameters.Top    = 50;
            }, ct2);

            return new MailboxDeltaPage
            {
                Items         = (response?.Value ?? new List<Message>()).Select(ToListDto).ToList(),
                NextDeltaLink = response?.OdataDeltaLink,
            };
        }, "DeltaSync", ct);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers — selects, mappers, retries, query-string surgery
    // ─────────────────────────────────────────────────────────────────────

    private static readonly string[] MessageSelect = new[]
    {
        "id", "subject", "from", "toRecipients", "ccRecipients",
        "sentDateTime", "receivedDateTime", "bodyPreview",
        "hasAttachments", "isRead", "conversationId", "internetMessageId",
    };

    private static readonly string[] MessageDetailSelect = new[]
    {
        "id", "subject", "from", "toRecipients", "ccRecipients", "bccRecipients",
        "sentDateTime", "receivedDateTime", "bodyPreview", "body",
        "hasAttachments", "isRead", "conversationId", "internetMessageId",
        "internetMessageHeaders",
    };

    private static Recipient ToRecipient(string address) => new()
    {
        EmailAddress = new EmailAddress { Address = address },
    };

    private static MailboxAddressDto? ToAddressDto(Recipient? r)
    {
        if (r?.EmailAddress is null || string.IsNullOrEmpty(r.EmailAddress.Address))
            return null;
        return new MailboxAddressDto
        {
            Name    = r.EmailAddress.Name,
            Address = r.EmailAddress.Address,
        };
    }

    private static MailboxMessageDto ToListDto(Message m) => new()
    {
        Id                = m.Id ?? string.Empty,
        ConversationId    = m.ConversationId ?? string.Empty,
        InternetMessageId = m.InternetMessageId ?? string.Empty,
        Subject           = m.Subject ?? string.Empty,
        BodyPreview       = m.BodyPreview ?? string.Empty,
        From              = ToAddressDto(m.From),
        ToRecipients      = (m.ToRecipients ?? new()).Select(ToAddressDto).Where(x => x != null).Select(x => x!).ToList(),
        CcRecipients      = (m.CcRecipients ?? new()).Select(ToAddressDto).Where(x => x != null).Select(x => x!).ToList(),
        SentAt            = m.SentDateTime?.UtcDateTime,
        ReceivedAt        = m.ReceivedDateTime?.UtcDateTime,
        HasAttachments    = m.HasAttachments ?? false,
        IsRead            = m.IsRead ?? false,
    };

    private static MailboxMessageDetailDto ToDetailDto(Message m)
    {
        var dto = new MailboxMessageDetailDto
        {
            Id                = m.Id ?? string.Empty,
            ConversationId    = m.ConversationId ?? string.Empty,
            InternetMessageId = m.InternetMessageId ?? string.Empty,
            Subject           = m.Subject ?? string.Empty,
            BodyPreview       = m.BodyPreview ?? string.Empty,
            From              = ToAddressDto(m.From),
            ToRecipients      = (m.ToRecipients  ?? new()).Select(ToAddressDto).Where(x => x != null).Select(x => x!).ToList(),
            CcRecipients      = (m.CcRecipients  ?? new()).Select(ToAddressDto).Where(x => x != null).Select(x => x!).ToList(),
            BccRecipients     = (m.BccRecipients ?? new()).Select(ToAddressDto).Where(x => x != null).Select(x => x!).ToList(),
            SentAt            = m.SentDateTime?.UtcDateTime,
            ReceivedAt        = m.ReceivedDateTime?.UtcDateTime,
            HasAttachments    = m.HasAttachments ?? false,
            IsRead            = m.IsRead ?? false,
        };

        if (m.Body is { } body)
        {
            if (body.ContentType == BodyType.Html)
                dto.BodyHtml = body.Content;
            else
                dto.BodyText = body.Content;
        }

        if (m.Attachments is { } atts)
        {
            dto.Attachments = atts.Select(a => new MailboxAttachmentDto
            {
                Id          = a.Id ?? string.Empty,
                Name        = a.Name ?? string.Empty,
                ContentType = a.ContentType,
                Size        = a.Size ?? 0,
                IsInline    = a.IsInline ?? false,
            }).ToList();
        }

        return dto;
    }

    /// <summary>
    /// Single-quote escape for OData filter literals ('it's → 'it''s').
    /// </summary>
    private static string EscapeOData(string value) => value.Replace("'", "''");

    /// <summary>
    /// Wraps a Graph call with retry on 429 / 503. Honors Retry-After (in
    /// seconds) when present; otherwise falls back to a fixed backoff. Other
    /// status codes propagate to the caller — a 401 means the policy is
    /// wrong and silent retry would mask the misconfiguration.
    /// </summary>
    private async Task<T> WithRetryAsync<T>(
        Func<CancellationToken, Task<T>> op,
        string opName,
        CancellationToken ct)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return await op(ct);
            }
            catch (ODataError ex) when (IsThrottle(ex) && attempt < MaxRetries)
            {
                attempt++;
                var delay = ParseRetryAfter(ex) ?? TimeSpan.FromSeconds(2 * attempt);
                _logger.LogWarning(
                    "GRAPH_THROTTLE op={Op} attempt={Attempt} status={Status} delaySec={Delay}",
                    opName, attempt, ex.ResponseStatusCode, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }
    }

    private static bool IsThrottle(ODataError ex) =>
        ex.ResponseStatusCode == (int)HttpStatusCode.TooManyRequests
        || ex.ResponseStatusCode == (int)HttpStatusCode.ServiceUnavailable;

    private static TimeSpan? ParseRetryAfter(ODataError ex)
    {
        // ODataError exposes ResponseHeaders as IDictionary<string, IEnumerable<string>>
        // when populated by Kiota — defensively probe for a numeric Retry-After.
        if (ex.ResponseHeaders is null) return null;
        if (!ex.ResponseHeaders.TryGetValue("Retry-After", out var values)) return null;
        var raw = values?.FirstOrDefault();
        if (string.IsNullOrEmpty(raw)) return null;
        if (int.TryParse(raw, out var seconds) && seconds > 0)
            return TimeSpan.FromSeconds(Math.Min(seconds, 60));
        return null;
    }
}
