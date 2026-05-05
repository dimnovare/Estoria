using Estoria.Application.Interfaces;
using Estoria.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Estoria.Infrastructure.Services;

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;
    private readonly string _fromEmail;
    private readonly string _notificationEmail;

    public ConsoleEmailService(IConfiguration config, ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;

        // Read the same Resend keys as ResendEmailService so dev logs surface
        // the actual From/To that prod would use. Empty string when unset is
        // fine here — dev is single-developer.
        var section = config.GetSection("Resend");
        _fromEmail         = section["FromEmail"]         ?? "";
        _notificationEmail = section["NotificationEmail"] ?? "";
    }

    public Task SendContactNotificationAsync(
        string name,
        string email,
        string message,
        string? phone,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL] Contact notification | From: {From} | To: {To} | Reply-To: {Name} <{Email}> | Phone: {Phone} | Message: {Message}",
            _fromEmail, _notificationEmail, name, email, phone ?? "(none)", message);

        return Task.CompletedTask;
    }

    public Task SendNewsletterWelcomeAsync(
        string email,
        Language lang,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL] Newsletter welcome | From: {From} | To: {To} | Language: {Language}",
            _fromEmail, email, lang);

        return Task.CompletedTask;
    }
}
