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

    /// <summary>
    /// Confirms a saved-search subscription. Includes the unsubscribe link so
    /// recipients can opt out from the very first email — keeps the list
    /// CAN-SPAM / GDPR clean without requiring a digest delivery first.
    /// </summary>
    Task SendSavedSearchConfirmationAsync(
        string toEmail,
        string? searchName,
        Language lang,
        string unsubscribeUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Periodic digest of new properties matching a saved search.
    /// <paramref name="matches"/> is a small projection of the property fields
    /// that should appear in the email — kept abstract so the email service
    /// doesn't depend on Application DTOs.
    /// </summary>
    Task SendSavedSearchDigestAsync(
        string toEmail,
        string? searchName,
        Language lang,
        IReadOnlyList<SavedSearchDigestItem> matches,
        string unsubscribeUrl,
        CancellationToken ct = default);
}

/// <summary>Single property line item rendered inside a saved-search digest.</summary>
public class SavedSearchDigestItem
{
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Url { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
}
