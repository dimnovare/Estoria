namespace Estoria.Application.DTOs.Team;

/// <summary>
/// Full admin-edit shape — all translations bundled in a single response so
/// the form can pre-populate every language tab. Keys are PascalCase
/// strings ("Et" / "En" / "Ru") so they round-trip cleanly between the
/// Language enum and the frontend without a lower/upper conversion step.
/// </summary>
public class TeamMemberAdminDetailDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Languages { get; set; } = [];
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Mirrors the shape of <c>CreateTeamMemberDto.Translations</c> — keyed
    /// on PascalCase Language string so the admin form can do a round-trip
    /// edit without any case-massaging.
    /// </summary>
    public Dictionary<string, TeamTranslationDto> Translations { get; set; } = new();
}
