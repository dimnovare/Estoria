using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

/// <summary>
/// A user-managed reminder/todo. Named "AppTask" rather than "Task" to avoid
/// colliding with <see cref="System.Threading.Tasks.Task"/> in service code.
/// Optional ContactId / DealId / PropertyId let the task hang off any of the
/// CRM entities — clicking through from a contact's tab list takes you here.
/// </summary>
public class AppTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Free-form notes. No length cap — long enough for a paste of an email thread is fine.</summary>
    public string? Description { get; set; }

    /// <summary>When the work is due. Always UTC; the UI converts to caller TZ.</summary>
    public DateTime DueAt { get; set; }

    public AppTaskStatus Status { get; set; } = AppTaskStatus.Pending;
    public AppTaskPriority Priority { get; set; } = AppTaskPriority.Normal;

    /// <summary>Owner of the task. Required.</summary>
    public Guid AssignedToUserId { get; set; }

    /// <summary>Audit trail — who created the task originally.</summary>
    public Guid CreatedByUserId { get; set; }

    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? PropertyId { get; set; }

    public DateTime? CompletedAt { get; set; }

    /// <summary>When the reminder notification should fire. Null = no reminder scheduled.</summary>
    public DateTime? ReminderAt { get; set; }

    /// <summary>True once the reminder has been delivered, so the job can short-circuit a re-fire.</summary>
    public bool ReminderSent { get; set; }

    /// <summary>
    /// Hangfire job id for the scheduled reminder. We persist it so reschedule/delete
    /// can cancel the prior fire via <see cref="Hangfire.IBackgroundJobClient.Delete(string)"/>.
    /// Null when no reminder is scheduled.
    /// </summary>
    public string? ReminderJobId { get; set; }

    /// <summary>
    /// Optional recurrence hint stored as a small RFC 5545-style fragment
    /// (e.g. "FREQ=DAILY", "FREQ=WEEKLY;BYDAY=MO"). The recurrence runner is a
    /// follow-up phase; for now this is metadata only.
    /// </summary>
    public string? Recurrence { get; set; }
}
