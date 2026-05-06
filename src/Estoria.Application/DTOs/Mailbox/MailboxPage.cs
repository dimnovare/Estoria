namespace Estoria.Application.DTOs.Mailbox;

/// <summary>
/// Cursor-style page used by Graph endpoints. Distinct from the offset/limit
/// <c>PagedResult</c> the rest of the CRM uses — Graph hands us an opaque
/// <c>@odata.nextLink</c> we extract a skipToken from.
/// </summary>
public class MailboxPage<T>
{
    public List<T> Items { get; set; } = [];

    /// <summary>Opaque continuation token. Null when there is no further page.</summary>
    public string? NextSkipToken { get; set; }
}
