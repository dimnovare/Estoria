namespace Estoria.Application.DTOs.Careers;

public class CareerListDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; }
}
