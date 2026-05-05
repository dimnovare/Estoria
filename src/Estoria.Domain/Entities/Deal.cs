using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class Deal : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional — null while the property is still being scouted.</summary>
    public Guid? PropertyId { get; set; }

    public Guid PrimaryContactId { get; set; }
    public Guid AssignedAgentId { get; set; }

    public DealStage Stage { get; set; } = DealStage.Lead;
    public DateTime StageChangedAt { get; set; } = DateTime.UtcNow;

    public TransactionType DealType { get; set; }
    public DealSide Side { get; set; }

    public DateOnly? ExpectedCloseDate { get; set; }
    public decimal? ExpectedValue { get; set; }
    public decimal? ActualValue { get; set; }
    public string Currency { get; set; } = "EUR";

    public decimal? CommissionPercent { get; set; }

    /// <summary>Required when Stage transitions to Lost.</summary>
    public string? LossReason { get; set; }

    public DateTime? WonAt { get; set; }
    public DateTime? LostAt { get; set; }

    public Property? Property { get; set; }
    public Contact PrimaryContact { get; set; } = null!;
    public User AssignedAgent { get; set; } = null!;

    public List<DealParticipant> Participants { get; set; } = [];
}
