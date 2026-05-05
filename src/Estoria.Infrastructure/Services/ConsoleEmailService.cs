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
        // the actual From/To that prod would use. A misconfigured value shows
        // up here instead of three weeks later when prod is finally tested.
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
        var subject = $"New Contact: {name}";
        var body    = BuildContactBody(name, email, phone, message);

        Log("ContactNotification", _notificationEmail, subject, body);
        return Task.CompletedTask;
    }

    public Task SendNewsletterWelcomeAsync(
        string email,
        Language lang,
        CancellationToken ct = default)
    {
        var (subject, body) = BuildNewsletterContent(lang);

        Log("NewsletterWelcome", email, subject, body);
        return Task.CompletedTask;
    }

    public Task SendBirthdayAsync(
        string toEmail,
        string toName,
        Language lang,
        string? subjectOverride = null,
        string? bodyOverride = null,
        CancellationToken ct = default)
    {
        var (subject, body) = BuildBirthdayContent(toName, lang, subjectOverride, bodyOverride);

        Log("Birthday", toEmail, subject, body);
        return Task.CompletedTask;
    }

    public Task SendSavedSearchConfirmationAsync(
        string toEmail,
        string? searchName,
        Language lang,
        string unsubscribeUrl,
        CancellationToken ct = default)
    {
        var (subject, body) = BuildSavedSearchConfirmationContent(searchName, lang, unsubscribeUrl);
        Log("SavedSearchConfirm", toEmail, subject, body);
        return Task.CompletedTask;
    }

    public Task SendSavedSearchDigestAsync(
        string toEmail,
        string? searchName,
        Language lang,
        IReadOnlyList<SavedSearchDigestItem> matches,
        string unsubscribeUrl,
        CancellationToken ct = default)
    {
        var (subject, body) = BuildSavedSearchDigestContent(searchName, lang, matches, unsubscribeUrl);
        Log("SavedSearchDigest", toEmail, subject, body);
        return Task.CompletedTask;
    }

    public Task<bool> SendNewsletterCampaignAsync(
        string toEmail,
        Language lang,
        string subject,
        string bodyHtml,
        string unsubscribeToken,
        CancellationToken ct = default)
    {
        // Dev-only: log and return true. Production path lives in
        // ResendEmailService where actual delivery happens.
        Log("NewsletterCampaign", toEmail, subject, bodyHtml);
        return Task.FromResult(true);
    }

    // -------------------------------------------------------------------------
    // Helpers — keep payload shape in sync with ResendEmailService so dev logs
    // mirror what would actually go on the wire in prod.
    // -------------------------------------------------------------------------

    private void Log(string kind, string to, string subject, string body)
    {
        // Single-line, grep-friendly. Same field shape (from=, to=, body=) as
        // RESEND_FAIL so both services are uniformly searchable.
        _logger.LogInformation(
            "[DEV EMAIL] from={From} to={To} subject=\"{Subject}\" kind={Kind} body={Body}",
            _fromEmail, to, subject, kind, Truncate(body, 200));
    }

    private static string BuildContactBody(string name, string email, string? phone, string message) =>
        $"""
        <h2>New Contact Form Submission</h2>
        <p><strong>Name:</strong> {HtmlEncode(name)}</p>
        <p><strong>Email:</strong> {HtmlEncode(email)}</p>
        <p><strong>Phone:</strong> {HtmlEncode(phone ?? "—")}</p>
        <hr/>
        <p><strong>Message:</strong></p>
        <p>{HtmlEncode(message).Replace("\n", "<br/>")}</p>
        """;

    private static (string subject, string body) BuildBirthdayContent(
        string toName, Language lang, string? subjectOverride, string? bodyOverride)
    {
        if (!string.IsNullOrWhiteSpace(subjectOverride) && !string.IsNullOrWhiteSpace(bodyOverride))
            return (subjectOverride, bodyOverride);

        var (subject, html) = lang switch
        {
            Language.Et => (
                "Palju õnne sünnipäevaks!",
                $"<p>{HtmlEncode(toName)}, soovime teile meeldejäävat sünnipäeva!</p>"),
            Language.Ru => (
                "С днём рождения!",
                $"<p>{HtmlEncode(toName)}, желаем вам прекрасного дня!</p>"),
            _ => (
                "Happy Birthday!",
                $"<p>{HtmlEncode(toName)}, wishing you a wonderful day!</p>"),
        };

        return (subjectOverride ?? subject, bodyOverride ?? html);
    }

    private static (string subject, string body) BuildSavedSearchConfirmationContent(
        string? searchName, Language lang, string unsubscribeUrl)
    {
        var label = string.IsNullOrWhiteSpace(searchName) ? "your search" : $"\"{HtmlEncode(searchName)}\"";
        var (subject, body) = lang switch
        {
            Language.Et => (
                "Otsing salvestatud — Estoria",
                $"<p>Salvestasime {label} ja saadame teile uusi sobivaid pakkumisi vastavalt sagedusele.</p>" +
                $"<p><a href=\"{unsubscribeUrl}\">Tühista tellimus</a></p>"),
            Language.Ru => (
                "Поиск сохранён — Estoria",
                $"<p>Мы сохранили {label} и будем присылать вам новые подходящие объекты.</p>" +
                $"<p><a href=\"{unsubscribeUrl}\">Отписаться</a></p>"),
            _ => (
                "Search saved — Estoria",
                $"<p>We've saved {label} and will email you matching properties on your chosen schedule.</p>" +
                $"<p><a href=\"{unsubscribeUrl}\">Unsubscribe</a></p>"),
        };
        return (subject, body);
    }

    private static (string subject, string body) BuildSavedSearchDigestContent(
        string? searchName, Language lang,
        IReadOnlyList<SavedSearchDigestItem> matches, string unsubscribeUrl)
    {
        var label = string.IsNullOrWhiteSpace(searchName) ? "your search" : $"\"{HtmlEncode(searchName)}\"";
        var items = string.Concat(matches.Select(m =>
            $"<li><a href=\"{HtmlEncode(m.Url)}\">{HtmlEncode(m.Title)}</a> — {m.Price:N0} {HtmlEncode(m.Currency)} ({HtmlEncode(m.City)})</li>"));

        var (subject, body) = lang switch
        {
            Language.Et => (
                $"Estoria: {matches.Count} uut pakkumist — {label}",
                $"<p>Uusi sobivaid pakkumisi: {matches.Count}</p><ul>{items}</ul>" +
                $"<p><a href=\"{unsubscribeUrl}\">Tühista tellimus</a></p>"),
            Language.Ru => (
                $"Estoria: {matches.Count} новых объектов — {label}",
                $"<p>Новых объектов: {matches.Count}</p><ul>{items}</ul>" +
                $"<p><a href=\"{unsubscribeUrl}\">Отписаться</a></p>"),
            _ => (
                $"Estoria: {matches.Count} new matches — {label}",
                $"<p>New matches found: {matches.Count}</p><ul>{items}</ul>" +
                $"<p><a href=\"{unsubscribeUrl}\">Unsubscribe</a></p>"),
        };
        return (subject, body);
    }

    private static (string subject, string body) BuildNewsletterContent(Language lang)
    {
        var (subject, html) = lang switch
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

        return (subject, $"<h2>{HtmlEncode(subject)}</h2>{html}");
    }

    private static string Truncate(string s, int max)
    {
        var oneLine = s.Replace("\r", " ").Replace("\n", " ");
        return oneLine.Length > max ? oneLine[..max] + "..." : oneLine;
    }

    private static string HtmlEncode(string text) =>
        System.Net.WebUtility.HtmlEncode(text);
}
