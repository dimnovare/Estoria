using System.ComponentModel.DataAnnotations;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.CRM.Deals;

public class DealWriteDto
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public Guid? PropertyId { get; set; }

    [Required]
    public Guid PrimaryContactId { get; set; }

    [Required]
    public Guid AssignedAgentId { get; set; }

    public TransactionType DealType { get; set; }
    public DealSide Side { get; set; }

    public DateOnly? ExpectedCloseDate { get; set; }
    public decimal? ExpectedValue { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal? CommissionPercent { get; set; }
}

/// <summary>
/// Atomic stage change. Won requires ActualValue; Lost requires LossReason —
/// both enforced server-side in DealService.ChangeStageAsync.
/// </summary>
public class ChangeStageDto
{
    [Required]
    public DealStage Stage { get; set; }
    public decimal? ActualValue { get; set; }
    public string? LossReason { get; set; }
    public string? Note { get; set; }
}
