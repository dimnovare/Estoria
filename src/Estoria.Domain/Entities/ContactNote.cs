using Estoria.Domain.Base;

namespace Estoria.Domain.Entities;

/// <summary>
/// Private agent notes on a contact. Distinct from Activity (which is the
/// shared timeline) and from Contact.Notes (a single short summary field).
/// </summary>
public class ContactNote : BaseEntity
{
    public Guid ContactId { get; set; }
    public Guid UserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsPinned { get; set; }

    public Contact Contact { get; set; } = null!;
    public User User { get; set; } = null!;
}
