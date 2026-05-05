using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Properties;

/// <summary>
/// Public-safe shape for the property history widget. Image URLs and
/// previous-agent ids are stripped before serialization — see
/// <see cref="Services.PropertyService.GetPublicHistoryAsync"/>.
/// </summary>
public class PropertyEventDto
{
    public Guid Id { get; set; }
    public PropertyEventType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PreviousJson { get; set; }
    public string? NewJson { get; set; }
}
