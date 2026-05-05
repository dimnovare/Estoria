using System.Net.Http.Json;
using Estoria.Application.Interfaces;
using Estoria.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Estoria.Infrastructure.Services;

public class ResendEmailService : IEmailService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _notificationEmail;

    public ResendEmailService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<ResendEmailService> logger)
    {
        _httpClientFactory   = httpClientFactory;
        _logger              = logger;

        var section          = config.GetSection("Resend");
        _apiKey              = section["ApiKey"]            ?? throw new InvalidOperationException("Resend:ApiKey is not configured.");
        _fromEmail           = section["FromEmail"]         ?? throw new InvalidOperationException("Resend:FromEmail is not configured.");
        _notificationEmail   = section["NotificationEmail"] ?? throw new InvalidOperationException("Resend:NotificationEmail is not configured.");
    }

    public async Task SendContactNotificationAsync(
        string name,
        string email,
        string message,
        string? phone,
        CancellationToken ct = default)
    {
        var html = $"""
            <h2>New Contact Form Submission</h2>
            <p><strong>Name:</strong> {HtmlEncode(name)}</p>
            <p><strong>Email:</strong> {HtmlEncode(email)}</p>
            <p><strong>Phone:</strong> {HtmlEncode(phone ?? "—")}</p>
            <hr/>
            <p><strong>Message:</strong></p>
            <p>{HtmlEncode(message).Replace("\n", "<br/>")}</p>
            """;

        await SendAsync(
            to: _notificationEmail,
            subject: $"New Contact: {name}",
            html: html,
            ct: ct);
    }

    public async Task SendNewsletterWelcomeAsync(
        string email,
        Language lang,
        CancellationToken ct = default)
    {
        var (subject, body) = lang switch
        {
            Language.Et => (
                "Tere tulemast Estoria uudiskirja!",
                "<p>Täname, et liitusite meie uudiskirjaga. Oleme varsti tagasi parimate kinnisvarapakkumistega!</p>"),
            Language.Ru => (
                "Добро пожаловать в рассылку Estoria!",
                "<p>Благодарим вас за подписку на нашу рассылку. Скоро мы пришлём вам лучшие предложения по недвижимости!</p>"),
            _ => (
                "Welcome to Estoria Newsletter!",
                "<p>Thank you for subscribing to our newsletter. We'll be in touch with the best property listings soon!</p>")
        };

        var html = $"<h2>{HtmlEncode(subject)}</h2>{body}";

        await SendAsync(to: email, subject: subject, html: html, ct: ct);
    }

    private async Task SendAsync(string to, string subject, string html, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Resend");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        var payload = new
        {
            from    = _fromEmail,
            to      = to,
            subject = subject,
            html    = html
        };

        var response = await client.PostAsJsonAsync("https://api.resend.com/emails", payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Resend API error {StatusCode}: {Body}",
                (int)response.StatusCode, body);

            // Grep-friendly single-line WARN — silent delivery failures were
            // hiding before. Truncate body to keep log lines bounded.
            var snippet = body.Length > 200 ? body[..200] : body;
            _logger.LogWarning(
                "RESEND_FAIL from={From} to={To} status={Status} body={Body}",
                _fromEmail, to, (int)response.StatusCode, snippet);
        }
    }

    private static string HtmlEncode(string text) =>
        System.Net.WebUtility.HtmlEncode(text);
}
