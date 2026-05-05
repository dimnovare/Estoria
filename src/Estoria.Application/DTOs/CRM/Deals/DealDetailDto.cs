using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.CRM.Deals;

public class DealDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DealStage Stage { get; set; }
    public DateTime StageChangedAt { get; set; }
    public TransactionType DealType { get; set; }
    public DealSide Side { get; set; }

    public Guid? PropertyId { get; set; }
    public Guid PrimaryContactId { get; set; }
    public string PrimaryContactName { get; set; } = string.Empty;
    public Guid AssignedAgentId { get; set; }
    public string AssignedAgentName { get; set; } = string.Empty;

    public DateOnly? ExpectedCloseDate { get; set; }
    public decimal? ExpectedValue { get; set; }
    public decimal? ActualValue { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal? CommissionPercent { get; set; }

    public string? LossReason { get; set; }
    public DateTime? WonAt { get; set; }
    public DateTime? LostAt { get; set; }

    public List<DealParticipantDto> Participants { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DealParticipantDto
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
