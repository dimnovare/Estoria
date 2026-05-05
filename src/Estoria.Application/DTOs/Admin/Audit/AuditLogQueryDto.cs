namespace Estoria.Application.DTOs.Admin.Audit;

/// <summary>
/// Filter + pagination args for GET /api/admin/audit-log.
/// All fields optional. Page size is clamped to [1, 200] in the service.
/// </summary>
public class AuditLogQueryDto
{
    public Guid? UserId { get; set; }

    /// <summary>Prefix match against Action — e.g. "Property." matches "Property.Create" etc.</summary>
    public string? ActionPrefix { get; set; }

    public string? EntityType { get; set; }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
