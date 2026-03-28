using System.ComponentModel.DataAnnotations;

namespace Estoria.Application.DTOs.Contact;

public class CreateContactDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? Subject { get; set; }

    [Required, MaxLength(5000)]
    public string Message { get; set; } = string.Empty;

    public Guid? PropertyId { get; set; }
}
