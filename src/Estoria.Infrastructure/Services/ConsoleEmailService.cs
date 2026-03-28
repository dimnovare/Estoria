using Estoria.Application.Interfaces;
using Estoria.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Estoria.Infrastructure.Services;

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger) => _logger = logger;

    public Task SendContactNotificationAsync(
        string name,
        string email,
        string message,
        string? phone,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL] Contact notification | From: {Name} <{Email}> | Phone: {Phone} | Message: {Message}",
            name, email, phone ?? "(none)", message);

        return Task.CompletedTask;
    }

    public Task SendNewsletterWelcomeAsync(
        string email,
        Language lang,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL] Newsletter welcome | To: {Email} | Language: {Language}",
            email, lang);

        return Task.CompletedTask;
    }
}
