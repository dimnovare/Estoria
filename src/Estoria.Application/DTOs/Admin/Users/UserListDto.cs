namespace Estoria.Application.DTOs.Admin.Users;

public class UserListDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = [];

    /// <summary>Profile photo URL — null when the user hasn't set one.</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>Spoken/written languages; empty array when unset.</summary>
    public string[] Languages { get; set; } = [];

    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
