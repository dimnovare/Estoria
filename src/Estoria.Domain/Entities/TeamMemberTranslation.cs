using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class TeamMemberTranslation : BaseEntity
{
    public Guid TeamMemberId { get; set; }
    public Language Language { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Bio { get; set; }

    public TeamMember TeamMember { get; set; } = null!;
}
