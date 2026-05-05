using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class BirthdayTemplateTranslation : BaseEntity
{
    public Guid BirthdayTemplateId { get; set; }
    public Language Language { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;

    public BirthdayTemplate BirthdayTemplate { get; set; } = null!;
}
