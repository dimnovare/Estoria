using Estoria.Domain.Base;

namespace Estoria.Domain.Entities;

/// <summary>
/// Singleton-style entity holding the email template that the daily birthday
/// job sends. Kept editable from the admin UI rather than baked into code so
/// marketing/agents can tweak copy without a deploy. Translations live on
/// <see cref="BirthdayTemplateTranslation"/>; one row per language.
/// </summary>
public class BirthdayTemplate : BaseEntity
{
    public ICollection<BirthdayTemplateTranslation> Translations { get; set; }
        = new List<BirthdayTemplateTranslation>();
}
