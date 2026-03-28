using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Newsletter;

public class SubscriberDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public Language Language { get; set; }
    public bool IsActive { get; set; }
    public DateTime SubscribedAt { get; set; }
}
