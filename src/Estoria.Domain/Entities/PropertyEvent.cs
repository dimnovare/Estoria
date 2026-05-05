using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

/// <summary>
/// Append-only audit of every meaningful change to a Property. Drives the
/// public "Price History" widget on PropertyDetail and the admin timeline.
/// PreviousJson / NewJson are small per-event blobs (the changed fields only,
/// not a full property snapshot) so the table stays compact.
/// </summary>
public class PropertyEvent : BaseEntity
{
    public Guid PropertyId { get; set; }

    public PropertyEventType Type { get; set; }

    public string? PreviousJson { get; set; }
    public string? NewJson { get; set; }

    /// <summary>Acting user. Null for system-generated events (e.g. recurring jobs).</summary>
    public Guid? UserId { get; set; }

    public Property Property { get; set; } = null!;
}
