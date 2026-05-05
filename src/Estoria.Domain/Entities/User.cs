using Estoria.Domain.Base;

namespace Estoria.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public Guid? TeamMemberId { get; set; }
    public List<string> Languages { get; set; } = [];

    public TeamMember? TeamMember { get; set; }
    public List<UserRoleAssignment> RoleAssignments { get; set; } = [];
}
