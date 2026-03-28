using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Contact;

public class ContactMessageDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public string? PropertyTitle { get; set; }
    public ContactStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
