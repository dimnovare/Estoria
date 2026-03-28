using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Newsletter;

public class SubscribeDto
{
    public string Email { get; set; } = string.Empty;
    public Language? Language { get; set; }
}
