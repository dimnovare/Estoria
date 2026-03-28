using Estoria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class PageContentConfiguration : IEntityTypeConfiguration<PageContent>
{
    public void Configure(EntityTypeBuilder<PageContent> builder)
    {
        builder.HasIndex(pc => pc.PageKey).IsUnique();

        builder.Property(pc => pc.PageKey).IsRequired();

        builder.HasMany(pc => pc.Translations)
            .WithOne(t => t.PageContent)
            .HasForeignKey(t => t.PageContentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PageContentTranslationConfiguration : IEntityTypeConfiguration<PageContentTranslation>
{
    public void Configure(EntityTypeBuilder<PageContentTranslation> builder)
    {
        builder.HasIndex(t => new { t.PageContentId, t.Language }).IsUnique();
    }
}
