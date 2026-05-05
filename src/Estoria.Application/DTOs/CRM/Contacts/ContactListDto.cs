using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.CRM.Contacts;

public class ContactListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public ContactSource Source { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }
    public bool IsBuyer { get; set; }
    public bool IsSeller { get; set; }
    public bool IsTenant { get; set; }
    public bool IsLandlord { get; set; }
    public bool IsPartner { get; set; }
    public string[] Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
