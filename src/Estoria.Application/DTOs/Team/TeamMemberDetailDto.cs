using Estoria.Application.DTOs.Properties;

namespace Estoria.Application.DTOs.Team;

public class TeamMemberDetailDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Languages { get; set; } = [];
    public List<PropertyListDto> Properties { get; set; } = [];
}
