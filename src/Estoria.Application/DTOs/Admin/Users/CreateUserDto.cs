using System.ComponentModel.DataAnnotations;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Admin.Users;

public class CreateUserDto
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Phone { get; set; }

    public string? PhotoUrl { get; set; }
    public List<string> Languages { get; set; } = [];
    public Guid? TeamMemberId { get; set; }

    [Required]
    public List<UserRole> Roles { get; set; } = [];
}
