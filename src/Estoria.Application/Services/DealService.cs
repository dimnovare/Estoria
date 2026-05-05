using Estoria.Application.Common;
using Estoria.Application.DTOs.CRM.Deals;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

public class DealService
{
    private readonly IAppDbContext _db;
    private readonly AuditService _audit;
    private readonly AuthorizationGuard _authz;

    public DealService(IAppDbContext db, AuditService audit, AuthorizationGuard authz)
    {
        _db    = db;
        _audit = audit;
        _authz = authz;
    }

    public async Task<List<DealListDto>> GetListAsync(
        DealStage? stage,
        Guid? agentId,
        Guid? contactId,
        CancellationToken ct = default)
    {
        IQueryable<Deal> q = _db.Deals.AsNoTracking()
            .Include(d => d.PrimaryContact)
            .Include(d => d.AssignedAgent);

        if (stage.HasValue)     q = q.Where(d => d.Stage == stage.Value);
        if (agentId.HasValue)   q = q.Where(d => d.AssignedAgentId == agentId.Value);
        if (contactId.HasValue) q = q.Where(d => d.PrimaryContactId == contactId.Value);

        var deals = await q
            .OrderByDescending(d => d.StageChangedAt)
            .ToListAsync(ct);

        return deals.Select(ToListDto).ToList();
    }

    /// <summary>Kanban view: deals grouped by stage. Empty stages still appear with an empty list.</summary>
    public async Task<Dictionary<string, List<DealListDto>>> GetKanbanAsync(
        Guid? agentId, CancellationToken ct = default)
    {
        var all = await GetListAsync(stage: null, agentId, contactId: null, ct);

        var byStage = Enum.GetValues<DealStage>()
            .ToDictionary(s => s.ToString(), _ => new List<DealListDto>());

        foreach (var d in all)
            byStage[d.Stage.ToString()].Add(d);

        return byStage;
    }

    public async Task<DealDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var d = await _db.Deals
            .AsNoTracking()
            .Include(x => x.PrimaryContact)
            .Include(x => x.AssignedAgent)
            .Include(x => x.Participants).ThenInclude(p => p.Contact)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return d is null ? null : ToDetailDto(d);
    }

    public async Task<Guid> CreateAsync(DealWriteDto dto, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        // Agents can create deals only on their own assignment.
        if (!_authz.IsAdmin)
            _authz.RequireOwnershipOrAdmin(dto.AssignedAgentId);

        var deal = new Deal
        {
            Title             = dto.Title.Trim(),
            PropertyId        = dto.PropertyId,
            PrimaryContactId  = dto.PrimaryContactId,
            AssignedAgentId   = dto.AssignedAgentId,
            DealType          = dto.DealType,
            Side              = dto.Side,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            ExpectedValue     = dto.ExpectedValue,
            Currency          = string.IsNullOrWhiteSpace(dto.Currency) ? "EUR" : dto.Currency.ToUpperInvariant(),
            CommissionPercent = dto.CommissionPercent,
            Stage             = DealStage.Lead,
            StageChangedAt    = DateTime.UtcNow,
        };

        _db.Deals.Add(deal);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Deal.Create",
            entityType: nameof(Deal),
            entityId: deal.Id,
            details: new { deal.Title, deal.AssignedAgentId, deal.PrimaryContactId, Stage = deal.Stage.ToString() },
            ct: ct);

        return deal.Id;
    }

    public async Task UpdateAsync(Guid id, DealWriteDto dto, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new KeyNotFoundException($"Deal {id} not found.");

        _authz.RequireOwnershipOrAdmin(deal.AssignedAgentId);

        deal.Title             = dto.Title.Trim();
        deal.PropertyId        = dto.PropertyId;
        deal.PrimaryContactId  = dto.PrimaryContactId;
        deal.AssignedAgentId   = dto.AssignedAgentId;
        deal.DealType          = dto.DealType;
        deal.Side              = dto.Side;
        deal.ExpectedCloseDate = dto.ExpectedCloseDate;
        deal.ExpectedValue     = dto.ExpectedValue;
        deal.Currency          = string.IsNullOrWhiteSpace(dto.Currency) ? "EUR" : dto.Currency.ToUpperInvariant();
        deal.CommissionPercent = dto.CommissionPercent;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Deal.Update",
            entityType: nameof(Deal),
            entityId: deal.Id,
            details: new { deal.Title, deal.AssignedAgentId },
            ct: ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new KeyNotFoundException($"Deal {id} not found.");

        _authz.RequireOwnershipOrAdmin(deal.AssignedAgentId);

        _db.Deals.Remove(deal);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Deal.Delete",
            entityType: nameof(Deal),
            entityId: deal.Id,
            details: new { deal.Title },
            ct: ct);
    }

    /// <summary>
    /// Atomic stage transition. Won requires ActualValue; Lost requires LossReason.
    /// Side-effect: writes a StageChange Activity row tied to the deal so the
    /// timeline shows the transition.
    /// </summary>
    public async Task ChangeStageAsync(Guid dealId, ChangeStageDto dto, CancellationToken ct = default)
    {
        _authz.RequireWriteAccess();

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == dealId, ct)
            ?? throw new KeyNotFoundException($"Deal {dealId} not found.");

        _authz.RequireOwnershipOrAdmin(deal.AssignedAgentId);

        if (deal.Stage == dto.Stage)
            return; // no-op transition — don't pollute the timeline

        // Terminal-state validations
        if (dto.Stage == DealStage.Won && !dto.ActualValue.HasValue)
            throw new ArgumentException("ActualValue is required when moving a deal to Won.");
        if (dto.Stage == DealStage.Lost && string.IsNullOrWhiteSpace(dto.LossReason))
            throw new ArgumentException("LossReason is required when moving a deal to Lost.");

        var fromStage = deal.Stage;
        deal.Stage          = dto.Stage;
        deal.StageChangedAt = DateTime.UtcNow;

        if (dto.Stage == DealStage.Won)
        {
            deal.WonAt       = DateTime.UtcNow;
            deal.ActualValue = dto.ActualValue;
            deal.LostAt      = null;
            deal.LossReason  = null;
        }
        else if (dto.Stage == DealStage.Lost)
        {
            deal.LostAt     = DateTime.UtcNow;
            deal.LossReason = dto.LossReason;
            deal.WonAt      = null;
        }
        else
        {
            // Re-opening a previously-Won/Lost deal — clear the terminal markers.
            deal.WonAt      = null;
            deal.LostAt     = null;
            deal.LossReason = null;
        }

        // Timeline row — visible alongside Notes/Calls/etc on the deal page.
        var note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
        var activity = new Activity
        {
            DealId     = deal.Id,
            ContactId  = deal.PrimaryContactId,
            PropertyId = deal.PropertyId,
            UserId     = _authz.CurrentUserId,
            Type       = ActivityType.StageChange,
            Title      = $"Stage: {fromStage} → {dto.Stage}",
            Body       = note,
            OccurredAt = DateTime.UtcNow,
        };
        // Add through DbSet so EF marks Added (BaseEntity ctor pre-populates Id;
        // adding through a tracked nav collection promotes to Modified).
        _db.Activities.Add(activity);

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Deal.StageChange",
            entityType: nameof(Deal),
            entityId: deal.Id,
            details: new { from = fromStage.ToString(), to = dto.Stage.ToString(), dto.ActualValue, dto.LossReason },
            ct: ct);
    }

    private static DealListDto ToListDto(Deal d) => new()
    {
        Id                 = d.Id,
        Title              = d.Title,
        Stage              = d.Stage,
        DealType           = d.DealType,
        Side               = d.Side,
        PropertyId         = d.PropertyId,
        PrimaryContactId   = d.PrimaryContactId,
        PrimaryContactName = d.PrimaryContact.FullName,
        AssignedAgentId    = d.AssignedAgentId,
        AssignedAgentName  = d.AssignedAgent.FullName,
        ExpectedCloseDate  = d.ExpectedCloseDate,
        ExpectedValue      = d.ExpectedValue,
        ActualValue        = d.ActualValue,
        Currency           = d.Currency,
        StageChangedAt     = d.StageChangedAt,
        CreatedAt          = d.CreatedAt,
    };

    private static DealDetailDto ToDetailDto(Deal d) => new()
    {
        Id                 = d.Id,
        Title              = d.Title,
        Stage              = d.Stage,
        StageChangedAt     = d.StageChangedAt,
        DealType           = d.DealType,
        Side               = d.Side,
        PropertyId         = d.PropertyId,
        PrimaryContactId   = d.PrimaryContactId,
        PrimaryContactName = d.PrimaryContact.FullName,
        AssignedAgentId    = d.AssignedAgentId,
        AssignedAgentName  = d.AssignedAgent.FullName,
        ExpectedCloseDate  = d.ExpectedCloseDate,
        ExpectedValue      = d.ExpectedValue,
        ActualValue        = d.ActualValue,
        Currency           = d.Currency,
        CommissionPercent  = d.CommissionPercent,
        LossReason         = d.LossReason,
        WonAt              = d.WonAt,
        LostAt             = d.LostAt,
        Participants       = d.Participants.Select(p => new DealParticipantDto
        {
            Id          = p.Id,
            ContactId   = p.ContactId,
            ContactName = p.Contact.FullName,
            Role        = p.Role,
        }).ToList(),
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
    };
}
