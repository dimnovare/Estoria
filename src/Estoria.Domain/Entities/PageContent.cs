using Estoria.Domain.Base;

namespace Estoria.Domain.Entities;

public class PageContent : BaseEntity
{
    public string PageKey { get; set; } = string.Empty;

    public List<PageContentTranslation> Translations { get; set; } = [];
}
