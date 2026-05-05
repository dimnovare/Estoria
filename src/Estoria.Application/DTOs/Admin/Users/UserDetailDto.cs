namespace Estoria.Application.DTOs.Admin.Users;

public class UserDetailDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
    public string[] Roles { get; set; } = [];
    public string[] Languages { get; set; } = [];
    public bool IsActive { get; set; }
    public Guid? TeamMemberId { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
