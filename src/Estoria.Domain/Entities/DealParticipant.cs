using Estoria.Domain.Base;

namespace Estoria.Domain.Entities;

/// <summary>
/// Additional contacts on a deal — co-buyer, lawyer, lender, partner-agent.
/// The deal's PrimaryContact is the canonical buyer/seller; this is everyone else.
/// </summary>
public class DealParticipant : BaseEntity
{
    public Guid DealId { get; set; }
    public Guid ContactId { get; set; }

    /// <summary>Free-form role — "co-buyer", "lawyer", "lender", "partner-agent", etc.</summary>
    public string Role { get; set; } = string.Empty;

    public Deal Deal { get; set; } = null!;
    public Contact Contact { get; set; } = null!;
}
