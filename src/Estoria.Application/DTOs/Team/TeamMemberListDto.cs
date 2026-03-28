namespace Estoria.Application.DTOs.Team;

public class TeamMemberListDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Languages { get; set; } = [];
}
