using System.ComponentModel.DataAnnotations;

namespace Estoria.Application.DTOs.CRM.Notes;

public class ContactNoteDto
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ContactNoteWriteDto
{
    [Required]
    public Guid ContactId { get; set; }

    [Required]
    public string Body { get; set; } = string.Empty;

    public bool IsPinned { get; set; }
}
