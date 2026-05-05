using System.ComponentModel.DataAnnotations;

namespace Estoria.Application.DTOs.Admin.Users;

public class UpdatePasswordDto
{
    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
