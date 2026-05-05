using System.ComponentModel.DataAnnotations;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.CRM.Tasks;

public class AppTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueAt { get; set; }
    public AppTaskStatus Status { get; set; }
    public AppTaskPriority Priority { get; set; }

    public Guid AssignedToUserId { get; set; }
    public Guid CreatedByUserId { get; set; }

    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? PropertyId { get; set; }

    public DateTime? CompletedAt { get; set; }
    public DateTime? ReminderAt { get; set; }
    public bool ReminderSent { get; set; }
    public string? Recurrence { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AppTaskWriteDto
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime DueAt { get; set; }

    public AppTaskPriority Priority { get; set; } = AppTaskPriority.Normal;

    /// <summary>
    /// Assignee. Optional on create — when null the service defaults to the
    /// caller (CreatedByUserId) so a user creating a personal todo can skip
    /// the field.
    /// </summary>
    public Guid? AssignedToUserId { get; set; }

    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? PropertyId { get; set; }

    public DateTime? ReminderAt { get; set; }

    [MaxLength(200)]
    public string? Recurrence { get; set; }
}

public class RescheduleDto
{
    [Required]
    public DateTime NewDueAt { get; set; }

    /// <summary>
    /// Optional new reminder time. When null, the existing reminder (if any) is
    /// kept in place; pass an explicit <c>null</c> to clear it via the dedicated
    /// update endpoint instead.
    /// </summary>
    public DateTime? NewReminderAt { get; set; }
}
