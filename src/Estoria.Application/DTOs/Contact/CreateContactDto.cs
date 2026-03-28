namespace Estoria.Application.DTOs.Contact;

public class CreateContactDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
}
