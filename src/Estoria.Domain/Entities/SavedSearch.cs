using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

/// <summary>
/// A saved property search subscription. Anonymous-first: only an email is
/// required, so visitors can subscribe without registering. <see cref="ContactId"/>
/// is filled in opportunistically when the email matches an existing CRM contact
/// (joined at create time and on demand). <see cref="UnsubscribeToken"/> is used
/// in the one-click /unsubscribe/{token} URL embedded in every digest.
/// </summary>
public class SavedSearch : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public Guid? ContactId { get; set; }

    public string? Name { get; set; }

    public Language PreferredLanguage { get; set; } = Language.En;

    /// <summary>
    /// Serialized search criteria — see <c>SavedSearchFilter</c> in the
    /// Application layer. Stored as a JSON string so the filter shape can grow
    /// without a schema change.
    /// </summary>
    public string FilterJson { get; set; } = "{}";

    public SavedSearchFrequency Frequency { get; set; } = SavedSearchFrequency.Daily;

    /// <summary>Last time the digest job dispatched results for this row. Null = never.</summary>
    public DateTime? LastSentAt { get; set; }

    public int LastResultsCount { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Random opaque token for the unsubscribe URL. 64 chars max.</summary>
    public string UnsubscribeToken { get; set; } = string.Empty;

    public Contact? Contact { get; set; }
}
