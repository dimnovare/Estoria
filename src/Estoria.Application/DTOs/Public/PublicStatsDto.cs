namespace Estoria.Application.DTOs.Public;

public class PublicStatsDto
{
    public int PropertiesActive { get; set; }
    public int SuccessfulDeals { get; set; }
    public int YearsExperience { get; set; }
    public int SatisfactionPercent { get; set; }
    public string[] Languages { get; set; } = [];
}
