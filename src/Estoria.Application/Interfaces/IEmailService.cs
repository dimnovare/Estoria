using Estoria.Domain.Enums;

namespace Estoria.Application.Interfaces;

public interface IEmailService
{
    Task SendContactNotificationAsync(
        string name,
        string email,
        string message,
        string? phone,
        CancellationToken ct = default);

    Task SendNewsletterWelcomeAsync(
        string email,
        Language lang,
        CancellationToken ct = default);
}
