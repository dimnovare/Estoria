using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class Contact : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? SecondaryPhone { get; set; }
    public Language PreferredLanguage { get; set; } = Language.En;

    /// <summary>Used by birthday-automation features. Date only — no time-zone math needed.</summary>
    public DateOnly? DateOfBirth { get; set; }

    public string? Address { get; set; }
    public string? Company { get; set; }
    public string? Position { get; set; }

    public ContactSource Source { get; set; } = ContactSource.Manual;
    public string? SourceDetail { get; set; }

    // Capability flags — a single contact can play multiple sides over time.
    public bool IsBuyer    { get; set; }
    public bool IsSeller   { get; set; }
    public bool IsPartner  { get; set; }
    public bool IsTenant   { get; set; }
    public bool IsLandlord { get; set; }

    /// <summary>Owning agent (User row). Nullable — unassigned contacts allowed.</summary>
    public Guid? AssignedAgentId { get; set; }

    /// <summary>Marketing-channel consent — required before newsletter or birthday email.</summary>
    public bool ConsentMarketing { get; set; }
    public DateTime? ConsentMarketingAt { get; set; }

    /// <summary>Short free-text summary — distinct from the ContactNote timeline entity.</summary>
    public string? Notes { get; set; }

    public List<string> Tags { get; set; } = [];

    public User? AssignedAgent { get; set; }
}
