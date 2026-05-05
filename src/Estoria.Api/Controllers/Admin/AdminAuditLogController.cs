using Estoria.Application.Common;
using Estoria.Application.DTOs.Admin.Audit;
using Estoria.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/audit-log")]
[Authorize(Roles = "Admin")]
public class AdminAuditLogController : ControllerBase
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize     = 200;

    private readonly IAppDbContext _db;

    public AdminAuditLogController(IAppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] AuditLogQueryDto query,
        CancellationToken ct = default)
    {
        var page     = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize <= 0 ? DefaultPageSize : query.PageSize, 1, MaxPageSize);

        var q = _db.AuditLog.AsNoTracking().AsQueryable();

        if (query.UserId is { } uid)
            q = q.Where(a => a.UserId == uid);

        if (!string.IsNullOrWhiteSpace(query.ActionPrefix))
            q = q.Where(a => a.Action.StartsWith(query.ActionPrefix));

        if (!string.IsNullOrWhiteSpace(query.EntityType))
            q = q.Where(a => a.EntityType == query.EntityType);

        if (query.From is { } from)
            q = q.Where(a => a.CreatedAt >= from);

        if (query.To is { } to)
            q = q.Where(a => a.CreatedAt <= to);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogEntryDto
            {
                Id          = a.Id,
                CreatedAt   = a.CreatedAt,
                UserId      = a.UserId,
                UserEmail   = a.UserEmail,
                Action      = a.Action,
                EntityType  = a.EntityType,
                EntityId    = a.EntityId,
                DetailsJson = a.DetailsJson,
                IpAddress   = a.IpAddress,
            })
            .ToListAsync(ct);

        return Ok(new PagedResult<AuditLogEntryDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize,
        });
    }
}
