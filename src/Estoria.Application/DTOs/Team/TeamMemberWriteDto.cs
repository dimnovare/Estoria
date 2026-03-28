using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Team;

public class TeamTranslationDto
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Bio { get; set; }
}

public class CreateTeamMemberDto
{
    public string? PhotoUrl { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Languages { get; set; } = [];
    public int SortOrder { get; set; }
    public Dictionary<Language, TeamTranslationDto> Translations { get; set; } = [];
}

public class UpdateTeamMemberDto : CreateTeamMemberDto { }
