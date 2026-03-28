using Estoria.Domain.Base;

namespace Estoria.Domain.Entities;

public class TeamMember : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Languages { get; set; } = [];
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public List<TeamMemberTranslation> Translations { get; set; } = [];
    public List<Property> Properties { get; set; } = [];
    public List<BlogPost> BlogPosts { get; set; } = [];
}
