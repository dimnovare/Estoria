using Estoria.Application.DTOs.Mailbox;

namespace Estoria.Application.Interfaces;

/// <summary>
/// Thin abstraction over Microsoft Graph for the info@estoria.estate shared
/// mailbox. Implementations talk to Graph using a client-credential flow
/// scoped via tenant Application Access Policy — the API never holds user
/// tokens, and the browser never sees Graph credentials.
/// </summary>
public interface IMailboxService
{
    /// <summary>
    /// Lists Inbox messages newest-first. <paramref name="skipToken"/> is the
    /// opaque cursor returned by a previous call; null fetches the first page.
    /// </summary>
    Task<MailboxPage<MailboxMessageDto>> ListInboxAsync(
        int top = 50,
        string? skipToken = null,
        bool unreadOnly = false,
        CancellationToken ct = default);

    /// <summary>Full message including body HTML and attachment manifest.</summary>
    Task<MailboxMessageDetailDto?> GetMessageAsync(
        string messageId,
        CancellationToken ct = default);

    /// <summary>
    /// Streams attachment bytes. Caller is responsible for disposing the
    /// returned stream.
    /// </summary>
    Task<MailboxAttachmentStream?> GetAttachmentAsync(
        string messageId,
        string attachmentId,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a message via Graph's sendMail action. Returns the
    /// internetMessageId of the sent message so the caller can persist it
    /// for threading. Note: Graph sendMail does not surface the message id
    /// directly; we read it back from the Sent Items folder when possible.
    /// </summary>
    Task<string?> SendAsync(
        SendMailboxMessageDto message,
        CancellationToken ct = default);

    Task MarkReadAsync(
        string messageId,
        bool isRead,
        CancellationToken ct = default);

    /// <summary>
    /// All messages on a conversation — both inbound (Inbox) and outbound
    /// (Sent Items). Used by the detail view to render the full thread.
    /// </summary>
    Task<MailboxPage<MailboxMessageDto>> ListConversationAsync(
        string conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Pulls the latest changes since the persisted delta cursor. Returns
    /// the new cursor + whatever messages came in. Implementations persist
    /// the cursor in SiteSettings under <c>graph.inbox_delta_link</c>.
    /// </summary>
    Task<MailboxDeltaPage> SyncInboxDeltaAsync(CancellationToken ct = default);
}

/// <summary>
/// Wraps a Graph attachment stream with the metadata the API needs to
/// proxy it to the browser. Caller disposes <see cref="Content"/>.
/// </summary>
public sealed class MailboxAttachmentStream : IDisposable
{
    public Stream Content { get; init; } = Stream.Null;
    public string FileName { get; init; } = "attachment";
    public string ContentType { get; init; } = "application/octet-stream";
    public long? Size { get; init; }

    public void Dispose() => Content.Dispose();
}

/// <summary>Result of one delta sync — new/changed messages and the cursor to persist.</summary>
public class MailboxDeltaPage
{
    public List<MailboxMessageDto> Items { get; set; } = [];
    public string? NextDeltaLink { get; set; }
}
