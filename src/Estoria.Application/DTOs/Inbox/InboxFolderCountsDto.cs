namespace Estoria.Application.DTOs.Inbox;

/// <summary>
/// Sidebar folder counts for the admin inbox. Backed by the MailboxLink table
/// — the Hangfire delta-sync keeps this in lock-step with Graph, so a fresh
/// count read doesn't burn a Graph quota call.
/// </summary>
public class InboxFolderCountsDto
{
    public int Inbox { get; set; }
    public int Unread { get; set; }
    public int Sent { get; set; }
    public int Archive { get; set; }
}
