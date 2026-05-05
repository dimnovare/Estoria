using System.Text.Json;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Estoria.Application.Services;

public class AuditService
{
    /// <summary>
    /// Cap stored JSON at 4 KB. Audit detail blobs are meant to be a glanceable
    /// summary of what changed — anything bigger is the wrong tool.
    /// </summary>
    private const int MaxDetailsLength = 4000;

    private static readonly JsonSerializerOptions DetailsJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Keep the payload small — audit log isn't a debugging surface.
        WriteIndented = false,
    };

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _db                  = db;
        _currentUser         = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        string? entityType = null,
        Guid? entityId = null,
        object? details = null,
        CancellationToken ct = default)
    {
        var entry = new AuditLogEntry
        {
            UserId      = _currentUser.UserId,
            UserEmail   = _currentUser.Email ?? "(system)",
            Action      = action,
            EntityType  = entityType,
            EntityId    = entityId,
            DetailsJson = SerializeDetails(details),
            IpAddress   = ResolveIp(),
        };

        _db.AuditLog.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    private static string? SerializeDetails(object? details)
    {
        if (details is null) return null;

        try
        {
            var json = JsonSerializer.Serialize(details, DetailsJsonOptions);
            return json.Length > MaxDetailsLength
                ? json[..MaxDetailsLength]
                : json;
        }
        catch
        {
            // If a caller hands us something unserializable (cyclic graphs, etc.),
            // record the failure type rather than throwing — audit is best-effort.
            return $"{{\"_serializeError\":\"{details.GetType().Name}\"}}";
        }
    }

    private string? ResolveIp()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return null;

        // X-Forwarded-For honored when set by an upstream proxy (Railway/Cloudflare).
        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            // First entry is the client; subsequent are the proxy chain.
            var first = forwarded.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(first)) return Truncate(first, 64);
        }

        var remote = ctx.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrEmpty(remote) ? null : Truncate(remote, 64);
    }

    private static string Truncate(string value, int max)
        => value.Length > max ? value[..max] : value;
}
