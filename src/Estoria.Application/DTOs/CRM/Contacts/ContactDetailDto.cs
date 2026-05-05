using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.CRM.Contacts;

public class ContactDetailDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? SecondaryPhone { get; set; }
    public Language PreferredLanguage { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? Company { get; set; }
    public string? Position { get; set; }
    public ContactSource Source { get; set; }
    public string? SourceDetail { get; set; }

    public bool IsBuyer { get; set; }
    public bool IsSeller { get; set; }
    public bool IsPartner { get; set; }
    public bool IsTenant { get; set; }
    public bool IsLandlord { get; set; }

    public Guid? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }

    public bool ConsentMarketing { get; set; }
    public DateTime? ConsentMarketingAt { get; set; }

    public string? Notes { get; set; }
    public string[] Tags { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
