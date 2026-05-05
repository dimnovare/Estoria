using System.ComponentModel.DataAnnotations;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.CRM.Contacts;

public class ContactWriteDto
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress, MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(50)]
    public string? SecondaryPhone { get; set; }

    public Language PreferredLanguage { get; set; } = Language.En;
    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(200)]
    public string? Company { get; set; }

    [MaxLength(200)]
    public string? Position { get; set; }

    public ContactSource Source { get; set; } = ContactSource.Manual;
    public string? SourceDetail { get; set; }

    public bool IsBuyer { get; set; }
    public bool IsSeller { get; set; }
    public bool IsPartner { get; set; }
    public bool IsTenant { get; set; }
    public bool IsLandlord { get; set; }

    public Guid? AssignedAgentId { get; set; }

    public bool ConsentMarketing { get; set; }

    public string? Notes { get; set; }
    public List<string> Tags { get; set; } = [];
}
