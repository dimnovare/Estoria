using System.ComponentModel.DataAnnotations;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Admin.Users;

public class UpdateUserDto
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Phone { get; set; }

    public string? PhotoUrl { get; set; }
    public List<string> Languages { get; set; } = [];
    public Guid? TeamMemberId { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Full replacement of the user's roles. Pass an empty list to drop all.</summary>
    [Required]
    public List<UserRole> Roles { get; set; } = [];
}
