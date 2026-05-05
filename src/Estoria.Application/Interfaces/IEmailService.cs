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

    /// <summary>
    /// Sends a birthday greeting. <paramref name="subjectOverride"/> /
    /// <paramref name="bodyOverride"/> let the caller supply pre-rendered copy
    /// (typically pulled from <c>BirthdayTemplate</c> for the contact's
    /// preferred language); when null, a built-in fallback is used.
    /// </summary>
    Task SendBirthdayAsync(
        string toEmail,
        string toName,
        Language lang,
        string? subjectOverride = null,
        string? bodyOverride = null,
        CancellationToken ct = default);
}
