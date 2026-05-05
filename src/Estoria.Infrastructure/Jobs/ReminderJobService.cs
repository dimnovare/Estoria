using Estoria.Application.Interfaces;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Estoria.Infrastructure.Jobs;

/// <summary>
/// Default reminder side-effect: log a structured line. Notification channels
/// (in-app feed, push, etc.) plug in here once they exist; until then the
/// console line is the receipt that the schedule fired correctly.
/// </summary>
public class ReminderJobService : IReminderJobService
{
    private readonly IAppDbContext _db;
    private readonly ILogger<ReminderJobService> _logger;

    public ReminderJobService(IAppDbContext db, ILogger<ReminderJobService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SendReminderAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null)
        {
            // Task deleted between schedule and fire — drop the job silently.
            _logger.LogInformation("Reminder fired for missing task {TaskId}", taskId);
            return;
        }

        if (task.ReminderSent)
        {
            _logger.LogDebug("Reminder for task {TaskId} already sent — skipping", taskId);
            return;
        }

        if (task.Status != AppTaskStatus.Pending)
        {
            _logger.LogDebug("Reminder for task {TaskId} skipped — status {Status}", taskId, task.Status);
            return;
        }

        // Single-line, grep-friendly. When a real notification channel lands,
        // dispatch from here; the structured fields stay the same.
        _logger.LogInformation(
            "TASK_REMINDER taskId={TaskId} assignedTo={AssignedTo} due={DueAt:o} title=\"{Title}\"",
            task.Id, task.AssignedToUserId, task.DueAt, task.Title);

        task.ReminderSent = true;
        await _db.SaveChangesAsync(ct);
    }
}
