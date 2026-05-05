using Estoria.Application.Common;
using Estoria.Application.DTOs.CRM.Tasks;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

/// <summary>
/// CRUD + scheduling for <see cref="AppTask"/>. Each task has an optional
/// reminder; we delegate the "fire at this DateTime" mechanic to Hangfire and
/// keep the resulting job id on the task row so reschedule/delete can cancel
/// the prior fire instead of letting it land twice.
/// </summary>
public class TaskService
{
    private readonly IAppDbContext _db;
    private readonly AuditService _audit;
    private readonly AuthorizationGuard _authz;
    private readonly IBackgroundJobClient _jobs;

    public TaskService(
        IAppDbContext db,
        AuditService audit,
        AuthorizationGuard authz,
        IBackgroundJobClient jobs)
    {
        _db    = db;
        _audit = audit;
        _authz = authz;
        _jobs  = jobs;
    }

    // ── Reads ─────────────────────────────────────────────────────────────────

    public async Task<AppTaskDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _db.Tasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? null : ToDto(t);
    }

    /// <summary>
    /// Tasks owned by the caller, optionally filtered by status and a "due
    /// within N days" window. Used by the /mine endpoint.
    /// </summary>
    public async Task<List<AppTaskDto>> GetMineAsync(
        AppTaskStatus? status = null,
        TimeSpan? dueWithin = null,
        bool? overdue = null,
        CancellationToken ct = default)
    {
        var userId = _authz.CurrentUserId;
        var q = _db.Tasks.AsNoTracking().Where(t => t.AssignedToUserId == userId);

        if (status.HasValue)
            q = q.Where(t => t.Status == status.Value);

        if (dueWithin.HasValue)
        {
            var cutoff = DateTime.UtcNow.Add(dueWithin.Value);
            q = q.Where(t => t.DueAt <= cutoff);
        }

        // Only filter when overdue=true. overdue=false would over-constrain
        // the "give me my upcoming tasks" use-case, so we treat it as omitted.
        if (overdue == true)
        {
            var now = DateTime.UtcNow;
            q = q.Where(t => t.Status == AppTaskStatus.Pending && t.DueAt < now);
        }

        var rows = await q.OrderBy(t => t.DueAt).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    /// <summary>
    /// Filter-rich list endpoint. Mirrors the AdminTasksController query
    /// surface — every filter is optional, so a no-arg call returns the
    /// most recent slice across all assignees. Pending tasks sort by
    /// soonest-due, completed/cancelled by most-recent-completion.
    /// </summary>
    public async Task<PagedResult<AppTaskDto>> GetListAsync(
        Guid? assignedToId,
        AppTaskStatus? status,
        AppTaskPriority? priority,
        bool? overdue,
        bool? hasReminder,
        DateTime? dueBefore,
        DateTime? dueAfter,
        Guid? contactId,
        Guid? dealId,
        Guid? propertyId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var q = _db.Tasks.AsNoTracking().AsQueryable();

        if (assignedToId.HasValue) q = q.Where(t => t.AssignedToUserId == assignedToId.Value);
        if (status.HasValue)       q = q.Where(t => t.Status == status.Value);
        if (priority.HasValue)     q = q.Where(t => t.Priority == priority.Value);
        if (contactId.HasValue)    q = q.Where(t => t.ContactId == contactId.Value);
        if (dealId.HasValue)       q = q.Where(t => t.DealId == dealId.Value);
        if (propertyId.HasValue)   q = q.Where(t => t.PropertyId == propertyId.Value);
        if (dueBefore.HasValue)    q = q.Where(t => t.DueAt < EnsureUtc(dueBefore.Value));
        if (dueAfter.HasValue)     q = q.Where(t => t.DueAt > EnsureUtc(dueAfter.Value));

        if (hasReminder == true)  q = q.Where(t => t.ReminderAt != null);
        if (hasReminder == false) q = q.Where(t => t.ReminderAt == null);

        if (overdue == true)
        {
            var now = DateTime.UtcNow;
            q = q.Where(t => t.Status == AppTaskStatus.Pending && t.DueAt < now);
        }

        var totalCount = await q.CountAsync(ct);

        // Pending → upcoming first; everything else sorts by recent completion
        // so the "Done" tab reads chronologically. We split the query into the
        // appropriate ORDER BY based on whether the caller asked for one of
        // the closed statuses.
        IQueryable<AppTask> ordered = status is AppTaskStatus.Done or AppTaskStatus.Cancelled
            ? q.OrderByDescending(t => t.CompletedAt ?? t.UpdatedAt)
            : q.OrderBy(t => t.DueAt);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AppTaskDto>
        {
            Items      = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize,
        };
    }

    public async Task<List<AppTaskDto>> GetForContactAsync(Guid contactId, CancellationToken ct = default)
    {
        var rows = await _db.Tasks
            .AsNoTracking()
            .Where(t => t.ContactId == contactId)
            .OrderBy(t => t.DueAt)
            .ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<List<AppTaskDto>> GetForDealAsync(Guid dealId, CancellationToken ct = default)
    {
        var rows = await _db.Tasks
            .AsNoTracking()
            .Where(t => t.DealId == dealId)
            .OrderBy(t => t.DueAt)
            .ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<List<AppTaskDto>> GetForPropertyAsync(Guid propertyId, CancellationToken ct = default)
    {
        var rows = await _db.Tasks
            .AsNoTracking()
            .Where(t => t.PropertyId == propertyId)
            .OrderBy(t => t.DueAt)
            .ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    // ── Writes ────────────────────────────────────────────────────────────────

    public async Task<Guid> CreateAsync(AppTaskWriteDto dto, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var creatorId = _authz.CurrentUserId;
        var task = new AppTask
        {
            Title             = dto.Title.Trim(),
            Description       = dto.Description,
            DueAt             = EnsureUtc(dto.DueAt),
            Priority          = dto.Priority,
            AssignedToUserId  = dto.AssignedToUserId ?? creatorId,
            CreatedByUserId   = creatorId,
            ContactId         = dto.ContactId,
            DealId            = dto.DealId,
            PropertyId        = dto.PropertyId,
            ReminderAt        = dto.ReminderAt is null ? null : EnsureUtc(dto.ReminderAt.Value),
            Recurrence        = dto.Recurrence,
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);

        // Schedule the reminder job after the row exists so the worker has a
        // valid id to load. Future: queue selection per task type ("default" today).
        if (task.ReminderAt is { } reminderAt)
            task.ReminderJobId = ScheduleReminder(task.Id, reminderAt);

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Task.Create",
            entityType: nameof(AppTask),
            entityId: task.Id,
            details: new { task.Title, task.DueAt, task.AssignedToUserId, task.ContactId, task.DealId, task.PropertyId },
            ct: ct);

        return task.Id;
    }

    public async Task UpdateAsync(Guid id, AppTaskWriteDto dto, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        // Owners (assignee) and admins may edit; everyone else is blocked.
        _authz.RequireOwnershipOrAdmin(task.AssignedToUserId);

        task.Title            = dto.Title.Trim();
        task.Description      = dto.Description;
        task.DueAt            = EnsureUtc(dto.DueAt);
        task.Priority         = dto.Priority;
        task.AssignedToUserId = dto.AssignedToUserId ?? task.AssignedToUserId;
        task.ContactId        = dto.ContactId;
        task.DealId           = dto.DealId;
        task.PropertyId       = dto.PropertyId;
        task.Recurrence       = dto.Recurrence;

        // Reminder reschedule: cancel the prior fire (if any) and schedule fresh.
        // Resetting ReminderSent so a moved-back-into-the-future reminder fires.
        var newReminder = dto.ReminderAt is null ? (DateTime?)null : EnsureUtc(dto.ReminderAt.Value);
        if (newReminder != task.ReminderAt)
        {
            CancelReminder(task);
            task.ReminderAt   = newReminder;
            task.ReminderSent = false;
            task.ReminderJobId = newReminder is { } r
                ? ScheduleReminder(task.Id, r)
                : null;
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Task.Update",
            entityType: nameof(AppTask),
            entityId: task.Id,
            details: new { task.Title, task.DueAt, task.Status },
            ct: ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        _authz.RequireOwnershipOrAdmin(task.AssignedToUserId);

        CancelReminder(task);

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Task.Delete",
            entityType: nameof(AppTask),
            entityId: task.Id,
            details: new { task.Title },
            ct: ct);
    }

    public async Task CompleteAsync(Guid id, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        _authz.RequireOwnershipOrAdmin(task.AssignedToUserId);

        if (task.Status == AppTaskStatus.Done) return;

        task.Status      = AppTaskStatus.Done;
        task.CompletedAt = DateTime.UtcNow;

        // No need to keep a reminder fire pending against a closed task.
        CancelReminder(task);
        task.ReminderJobId = null;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Task.Complete",
            entityType: nameof(AppTask),
            entityId: task.Id,
            ct: ct);
    }

    /// <summary>
    /// Used by the optimistic toggle in the admin task list. Differs from
    /// CompleteAsync in that it accepts any target status and clears /
    /// stamps CompletedAt accordingly.
    /// </summary>
    public async Task SetStatusAsync(Guid id, AppTaskStatus status, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        _authz.RequireOwnershipOrAdmin(task.AssignedToUserId);

        if (task.Status == status) return;

        task.Status = status;
        task.CompletedAt = status == AppTaskStatus.Done ? DateTime.UtcNow : null;

        // Closed tasks shouldn't keep firing reminders. Re-opening (Done →
        // Pending) leaves ReminderAt as-is but unmarks ReminderSent so the
        // user can manually re-schedule via the edit flow.
        if (status != AppTaskStatus.Pending)
        {
            CancelReminder(task);
            task.ReminderJobId = null;
        }
        else
        {
            task.ReminderSent = false;
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Task.SetStatus",
            entityType: nameof(AppTask),
            entityId: task.Id,
            details: new { task.Status },
            ct: ct);
    }

    public async Task RescheduleAsync(Guid id, RescheduleDto dto, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} not found.");

        _authz.RequireOwnershipOrAdmin(task.AssignedToUserId);

        task.DueAt = EnsureUtc(dto.NewDueAt);

        if (dto.NewReminderAt.HasValue)
        {
            CancelReminder(task);
            task.ReminderAt    = EnsureUtc(dto.NewReminderAt.Value);
            task.ReminderSent  = false;
            task.ReminderJobId = ScheduleReminder(task.Id, task.ReminderAt.Value);
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Task.Reschedule",
            entityType: nameof(AppTask),
            entityId: task.Id,
            details: new { task.DueAt, task.ReminderAt },
            ct: ct);
    }

    // ── Hangfire helpers ──────────────────────────────────────────────────────

    private string? ScheduleReminder(Guid taskId, DateTime reminderAt)
    {
        // Hangfire uses DateTimeOffset under the hood; pass UTC explicitly so
        // local-vs-utc clock interpretation can't drift.
        var when = new DateTimeOffset(reminderAt, TimeSpan.Zero);

        // Past / negative offsets schedule for "as soon as possible" — Hangfire
        // handles that gracefully but log so we notice in dev.
        return _jobs.Schedule<IReminderJobService>(svc => svc.SendReminderAsync(taskId, default), when);
    }

    private void CancelReminder(AppTask task)
    {
        if (string.IsNullOrEmpty(task.ReminderJobId)) return;
        _jobs.Delete(task.ReminderJobId);
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc         => value,
            DateTimeKind.Local       => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _                        => value,
        };

    private static AppTaskDto ToDto(AppTask t) => new()
    {
        Id               = t.Id,
        Title            = t.Title,
        Description      = t.Description,
        DueAt            = t.DueAt,
        Status           = t.Status,
        Priority         = t.Priority,
        AssignedToUserId = t.AssignedToUserId,
        CreatedByUserId  = t.CreatedByUserId,
        ContactId        = t.ContactId,
        DealId           = t.DealId,
        PropertyId       = t.PropertyId,
        CompletedAt      = t.CompletedAt,
        ReminderAt       = t.ReminderAt,
        ReminderSent     = t.ReminderSent,
        Recurrence       = t.Recurrence,
        CreatedAt        = t.CreatedAt,
        UpdatedAt        = t.UpdatedAt,
    };
}
