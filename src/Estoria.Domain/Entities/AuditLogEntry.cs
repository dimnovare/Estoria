using Estoria.Domain.Base;

namespace Estoria.Domain.Entities;

public class AuditLogEntry : BaseEntity
{
    /// <summary>Acting user. Null for system actions (e.g. seeder, scheduled jobs).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Denormalized so the row is readable even if the User row is later deleted.</summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>Dotted action key, e.g. "Property.Create", "Auth.Login", "Auth.LoginFailed".</summary>
    public string Action { get; set; } = string.Empty;

    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }

    /// <summary>Small JSON blob — capped to 4000 chars by AuditService.</summary>
    public string? DetailsJson { get; set; }

    public string? IpAddress { get; set; }
}
