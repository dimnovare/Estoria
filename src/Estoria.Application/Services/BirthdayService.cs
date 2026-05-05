using Estoria.Application.DTOs.CRM.Birthday;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Estoria.Application.Services;

/// <summary>
/// Daily birthday automation. The recurring Hangfire job calls
/// <see cref="SendBirthdayGreetingsAsync"/> at 08:00 UTC; the same method is
/// reused by the admin "send-now" controller for staging tests.
///
/// The <c>birthday.auto_send</c> SiteSetting is the off-switch: while it's
/// "false" (the default — see <c>DataSeeder.SeedSiteSettingsAsync</c>) the
/// service computes the recipient list and logs "would send to N contacts"
/// without dispatching any email. Flip it to "true" only after a dry-run
/// against today's data.
/// </summary>
public class BirthdayService
{
    private readonly IAppDbContext _db;
    private readonly IEmailService _email;
    private readonly AuditService _audit;
    private readonly SiteSettingService _settings;
    private readonly ILogger<BirthdayService> _logger;

    public BirthdayService(
        IAppDbContext db,
        IEmailService email,
        AuditService audit,
        SiteSettingService settings,
        ILogger<BirthdayService> logger)
    {
        _db       = db;
        _email    = email;
        _audit    = audit;
        _settings = settings;
        _logger   = logger;
    }

    // ── Reads ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Contacts whose DateOfBirth month/day matches today (UTC) and who have
    /// opted in to marketing email. Year is intentionally ignored — birthdays
    /// repeat annually, not on the year of birth.
    /// </summary>
    public async Task<List<Contact>> GetBirthdaysTodayAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await GetBirthdaysOnAsync(today.Month, today.Day, ct);
    }

    public async Task<List<UpcomingBirthdayDto>> GetUpcomingAsync(int days, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 90);

        // Pull the candidate set once, then sort/filter in memory — birthdays
        // are sparse so the row count is small. Doing month/day arithmetic on
        // the server keeps the query simple and DB-portable.
        var candidates = await _db.Contacts
            .AsNoTracking()
            .Where(c => c.DateOfBirth != null)
            .Select(c => new
            {
                c.Id,
                c.FullName,
                c.Email,
                c.DateOfBirth,
                c.PreferredLanguage,
                c.ConsentMarketing,
            })
            .ToListAsync(ct);

        var today = DateTime.UtcNow.Date;
        var results = new List<UpcomingBirthdayDto>();

        foreach (var c in candidates)
        {
            var dob = c.DateOfBirth!.Value;
            var (daysUntil, nextBirthday) = ComputeNext(today, dob);
            if (daysUntil > days) continue;

            results.Add(new UpcomingBirthdayDto
            {
                ContactId         = c.Id,
                FullName          = c.FullName,
                Email             = c.Email,
                DateOfBirth       = dob,
                PreferredLanguage = c.PreferredLanguage,
                ConsentMarketing  = c.ConsentMarketing,
                DaysUntil         = daysUntil,
                NextBirthday      = nextBirthday,
                TurningAge        = nextBirthday.Year - dob.Year,
            });
        }

        return results.OrderBy(r => r.DaysUntil).ThenBy(r => r.FullName).ToList();
    }

    // ── Send ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Renders today's birthdays and (when auto-send is on) dispatches the
    /// greeting per contact. Emits an Activity row + AuditLog entry for every
    /// successful send so the timeline shows the contact-touch.
    /// </summary>
    public async Task<BirthdaySendResultDto> SendBirthdayGreetingsAsync(CancellationToken ct = default)
    {
        var autoSend = await _settings.GetBoolAsync("birthday.auto_send", false, ct);
        var contacts = await GetBirthdaysTodayAsync(ct);

        // Eligible = will-send if the switch is on. Filter out contacts without
        // an email or marketing consent; those are recoverable misconfigs, not
        // failures, so we count them as "skipped" rather than throwing.
        var eligible = contacts
            .Where(c => !string.IsNullOrWhiteSpace(c.Email) && c.ConsentMarketing)
            .ToList();

        var skipped = contacts.Count - eligible.Count;

        if (!autoSend)
        {
            _logger.LogInformation(
                "BIRTHDAY_DRYRUN would send to {Count} contacts (auto-send disabled)",
                eligible.Count);

            return new BirthdaySendResultDto
            {
                Eligible        = eligible.Count,
                Sent            = 0,
                Skipped         = skipped,
                AutoSendEnabled = false,
            };
        }

        var template = await LoadTemplateAsync(ct);
        var sent = 0;

        foreach (var contact in eligible)
        {
            var rendered = RenderForContact(template, contact);
            try
            {
                await _email.SendBirthdayAsync(
                    toEmail:         contact.Email!,
                    toName:          contact.FullName,
                    lang:            contact.PreferredLanguage,
                    subjectOverride: rendered?.Subject,
                    bodyOverride:    rendered?.BodyHtml,
                    ct:              ct);

                // Activity.UserId FK is Restrict, so we need a real user. Prefer
                // the assigned agent; if the contact has none, skip the timeline
                // row but keep the audit log + email send. (When we add a "system"
                // user, point this fallback there.)
                if (contact.AssignedAgentId is { } agentId)
                {
                    _db.Activities.Add(new Activity
                    {
                        ContactId  = contact.Id,
                        UserId     = agentId,
                        Type       = ActivityType.SystemEvent,
                        Title      = "Birthday greeting sent",
                        OccurredAt = DateTime.UtcNow,
                    });
                    await _db.SaveChangesAsync(ct);
                }

                await _audit.LogAsync(
                    "Birthday.Send",
                    entityType: nameof(Contact),
                    entityId: contact.Id,
                    details: new { contact.Email, contact.PreferredLanguage },
                    ct: ct);

                sent++;
            }
            catch (Exception ex)
            {
                // Don't let one failed send abort the rest of the run. The
                // ResendEmailService already grep-logs delivery failures with
                // RESEND_FAIL; we add the contact id for traceability.
                _logger.LogWarning(ex,
                    "BIRTHDAY_SEND_FAIL contactId={ContactId} email={Email}",
                    contact.Id, contact.Email);

                skipped++;
            }
        }

        _logger.LogInformation(
            "BIRTHDAY_RUN eligible={Eligible} sent={Sent} skipped={Skipped}",
            eligible.Count, sent, skipped);

        return new BirthdaySendResultDto
        {
            Eligible        = eligible.Count,
            Sent            = sent,
            Skipped         = skipped,
            AutoSendEnabled = true,
        };
    }

    /// <summary>
    /// Send-now for a single contact. Reuses the same eligibility rules as
    /// the bulk path (consent, valid email, DOB known) so admins can't ship
    /// a greeting to a contact who hasn't opted in.
    /// </summary>
    public async Task<BirthdaySendResultDto> SendOneAsync(Guid contactId, CancellationToken ct = default)
    {
        var autoSend = await _settings.GetBoolAsync("birthday.auto_send", false, ct);

        var contact = await _db.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contactId, ct);

        if (contact is null
            || contact.DateOfBirth is null
            || string.IsNullOrWhiteSpace(contact.Email)
            || !contact.ConsentMarketing)
        {
            return new BirthdaySendResultDto
            {
                Eligible        = 0,
                Sent            = 0,
                Skipped         = 1,
                AutoSendEnabled = autoSend,
            };
        }

        if (!autoSend)
        {
            _logger.LogInformation(
                "BIRTHDAY_DRYRUN_ONE contactId={ContactId} (auto-send disabled)",
                contactId);
            return new BirthdaySendResultDto
            {
                Eligible        = 1,
                Sent            = 0,
                Skipped         = 0,
                AutoSendEnabled = false,
            };
        }

        var template = await LoadTemplateAsync(ct);
        var rendered = RenderForContact(template, contact);

        try
        {
            await _email.SendBirthdayAsync(
                toEmail:         contact.Email!,
                toName:          contact.FullName,
                lang:            contact.PreferredLanguage,
                subjectOverride: rendered?.Subject,
                bodyOverride:    rendered?.BodyHtml,
                ct:              ct);

            await _audit.LogAsync(
                "Birthday.SendOne",
                entityType: nameof(Contact),
                entityId: contact.Id,
                details: new { contact.Email, contact.PreferredLanguage },
                ct: ct);

            return new BirthdaySendResultDto
            {
                Eligible        = 1,
                Sent            = 1,
                Skipped         = 0,
                AutoSendEnabled = true,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "BIRTHDAY_SEND_ONE_FAIL contactId={ContactId} email={Email}",
                contact.Id, contact.Email);
            return new BirthdaySendResultDto
            {
                Eligible        = 1,
                Sent            = 0,
                Skipped         = 1,
                AutoSendEnabled = true,
            };
        }
    }

    // ── Template management ───────────────────────────────────────────────────

    /// <summary>
    /// Always returns three rows (one per <see cref="Language"/> value),
    /// filling empty strings for languages the editor hasn't customized.
    /// Lets the frontend bind a tab per language without conditional logic.
    /// </summary>
    public async Task<List<BirthdayTemplateTranslationDto>> GetTranslationsAsync(CancellationToken ct = default)
    {
        var tpl = await _db.BirthdayTemplates
            .AsNoTracking()
            .Include(t => t.Translations)
            .FirstOrDefaultAsync(ct);

        var existing = tpl?.Translations
            .ToDictionary(t => t.Language, t => t)
            ?? new Dictionary<Language, BirthdayTemplateTranslation>();

        var langs = new[] { Language.Et, Language.En, Language.Ru };
        return langs.Select(lang => existing.TryGetValue(lang, out var t)
            ? new BirthdayTemplateTranslationDto
            {
                Language = lang,
                Subject  = t.Subject,
                BodyHtml = t.BodyHtml,
            }
            : new BirthdayTemplateTranslationDto
            {
                Language = lang,
                Subject  = string.Empty,
                BodyHtml = string.Empty,
            }).ToList();
    }

    /// <summary>
    /// Single-language upsert. Creates the singleton template row on first
    /// call so editors don't need to seed three entries before seeing any
    /// effect.
    /// </summary>
    public async Task UpsertTranslationAsync(
        Language language, string subject, string bodyHtml, CancellationToken ct = default)
    {
        var tpl = await _db.BirthdayTemplates
            .Include(t => t.Translations)
            .FirstOrDefaultAsync(ct);

        if (tpl is null)
        {
            tpl = new BirthdayTemplate();
            _db.BirthdayTemplates.Add(tpl);
            await _db.SaveChangesAsync(ct);
        }

        var existing = tpl.Translations.FirstOrDefault(x => x.Language == language);
        if (existing is null)
        {
            tpl.Translations.Add(new BirthdayTemplateTranslation
            {
                BirthdayTemplateId = tpl.Id,
                Language           = language,
                Subject            = subject ?? string.Empty,
                BodyHtml           = bodyHtml ?? string.Empty,
            });
        }
        else
        {
            existing.Subject  = subject ?? string.Empty;
            existing.BodyHtml = bodyHtml ?? string.Empty;
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Birthday.UpdateTemplate",
            entityType: nameof(BirthdayTemplate),
            entityId: tpl.Id,
            details: new { Language = language.ToString() },
            ct: ct);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private async Task<List<Contact>> GetBirthdaysOnAsync(int month, int day, CancellationToken ct)
    {
        // SQL-side filter on month/day to avoid pulling every contact with a
        // DOB; the partial index on DateOfBirth keeps the scan small even at scale.
        return await _db.Contacts
            .AsNoTracking()
            .Where(c => c.DateOfBirth != null
                     && c.DateOfBirth!.Value.Month == month
                     && c.DateOfBirth!.Value.Day   == day)
            .ToListAsync(ct);
    }

    private async Task<BirthdayTemplate?> LoadTemplateAsync(CancellationToken ct)
        => await _db.BirthdayTemplates
            .AsNoTracking()
            .Include(t => t.Translations)
            .FirstOrDefaultAsync(ct);

    private static (string Subject, string BodyHtml)? RenderForContact(
        BirthdayTemplate? template, Contact contact)
    {
        if (template is null) return null;

        // Prefer the contact's preferred language; fall back to any English
        // translation, then any translation at all. Lets us add languages
        // without breaking older opted-in contacts.
        var t = template.Translations.FirstOrDefault(x => x.Language == contact.PreferredLanguage)
             ?? template.Translations.FirstOrDefault(x => x.Language == Language.En)
             ?? template.Translations.FirstOrDefault();

        if (t is null) return null;

        // {{name}} is the only token for now. Substitute on body and subject so
        // marketing copy can address the recipient personally.
        var subject = t.Subject.Replace("{{name}}", contact.FullName);
        var body    = t.BodyHtml.Replace("{{name}}", contact.FullName);
        return (subject, body);
    }

    /// <summary>
    /// Computes the next anniversary date (this year or next) plus days until.
    /// Feb-29 birthdays clamp to the last valid day of the target month.
    /// </summary>
    private static (int DaysUntil, DateOnly NextBirthday) ComputeNext(DateTime today, DateOnly dob)
    {
        var year      = today.Year;
        var nextMonth = dob.Month;
        var nextDay   = Math.Min(dob.Day, DateTime.DaysInMonth(year, nextMonth));
        var thisYearAnniv = new DateTime(year, nextMonth, nextDay, 0, 0, 0, DateTimeKind.Utc);

        if (thisYearAnniv < today)
        {
            year++;
            nextDay = Math.Min(dob.Day, DateTime.DaysInMonth(year, nextMonth));
            thisYearAnniv = new DateTime(year, nextMonth, nextDay, 0, 0, 0, DateTimeKind.Utc);
        }

        var daysUntil = (int)(thisYearAnniv - today).TotalDays;
        return (daysUntil, DateOnly.FromDateTime(thisYearAnniv));
    }
}
