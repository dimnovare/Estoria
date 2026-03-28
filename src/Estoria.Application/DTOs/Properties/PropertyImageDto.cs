namespace Estoria.Application.DTOs.Properties;

public class PropertyImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
}
