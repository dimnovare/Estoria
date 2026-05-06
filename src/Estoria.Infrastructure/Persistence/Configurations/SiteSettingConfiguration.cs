using Estoria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.HasIndex(s => s.Key).IsUnique();

        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasMany(s => s.Translations)
            .WithOne(t => t.SiteSetting)
            .HasForeignKey(t => t.SiteSettingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SiteSettingTranslationConfiguration : IEntityTypeConfiguration<SiteSettingTranslation>
{
    public void Configure(EntityTypeBuilder<SiteSettingTranslation> builder)
    {
        builder.Property(t => t.Value).IsRequired();

        // Composite uniqueness: one row per (setting, language). Plus the
        // covering index implicitly used by per-key/per-language reads.
        builder.HasIndex(t => new { t.SiteSettingId, t.Language }).IsUnique();
    }
}
