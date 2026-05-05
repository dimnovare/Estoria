using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class Activity : BaseEntity
{
    // All three nullable — an activity can attach to any combination.
    // System-generated activities (e.g. StageChange) typically attach to a Deal only.
    public Guid? DealId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? PropertyId { get; set; }

    /// <summary>The user who performed/owns the activity. Required.</summary>
    public Guid UserId { get; set; }

    public ActivityType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public int? DurationMinutes { get; set; }
    public string? Outcome { get; set; }

    public Deal? Deal { get; set; }
    public Contact? Contact { get; set; }
    public Property? Property { get; set; }
    public User User { get; set; } = null!;
}
