namespace Estoria.Application.Interfaces;

/// <summary>
/// Reminder execution surface called by Hangfire when a task's ReminderAt fires.
/// Lives in Application so <c>TaskService</c> can <c>BackgroundJob.Schedule</c>
/// against the interface without dragging Hangfire wiring into the schedulers.
/// The concrete implementation lives in Infrastructure.
/// </summary>
public interface IReminderJobService
{
    /// <summary>
    /// Fires the in-app/notification side-effects for a task whose ReminderAt
    /// has come due. Idempotent — if the task has already been delivered,
    /// completed, cancelled, or deleted, the call is a no-op.
    /// </summary>
    Task SendReminderAsync(Guid taskId, CancellationToken ct = default);
}
