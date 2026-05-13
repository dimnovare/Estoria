using Estoria.Application.DTOs.Mailbox;
using Estoria.Application.Interfaces;

namespace Estoria.Infrastructure.External;

/// <summary>
/// Stand-in mailbox service used in development when Graph is not configured.
/// All read methods return empty pages; write methods are no-ops. The admin
/// inbox UI renders cleanly (zero messages) instead of blowing up at controller
/// activation time when GraphMailService's constructor would have thrown.
/// </summary>
public class NoOpMailboxService : IMailboxService
{
    public Task<MailboxPage<MailboxMessageDto>> ListInboxAsync(
        string folder = "inbox", int top = 50, string? skipToken = null,
        bool unreadOnly = false, bool hasAttachments = false,
        CancellationToken ct = default)
        => Task.FromResult(new MailboxPage<MailboxMessageDto>
        {
            Items = new List<MailboxMessageDto>(),
            NextSkipToken = null,
        });

    public Task<MailboxMessageDetailDto?> GetMessageAsync(
        string messageId, CancellationToken ct = default)
        => Task.FromResult<MailboxMessageDetailDto?>(null);

    public Task<MailboxAttachmentStream?> GetAttachmentAsync(
        string messageId, string attachmentId, CancellationToken ct = default)
        => Task.FromResult<MailboxAttachmentStream?>(null);

    public Task<string?> SendAsync(SendMailboxMessageDto message, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    public Task MarkReadAsync(string messageId, bool isRead, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<MailboxPage<MailboxMessageDto>> ListConversationAsync(
        string conversationId, CancellationToken ct = default)
        => Task.FromResult(new MailboxPage<MailboxMessageDto>
        {
            Items = new List<MailboxMessageDto>(),
            NextSkipToken = null,
        });

    public Task<MailboxDeltaPage> SyncInboxDeltaAsync(CancellationToken ct = default)
        => Task.FromResult(new MailboxDeltaPage
        {
            Items = new List<MailboxMessageDto>(),
            NextDeltaLink = null,
        });
}
