using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => new { p.Status, p.IsFeatured });

        builder.Property(p => p.Slug).IsRequired();
        builder.Property(p => p.Currency).HasDefaultValue("EUR");
        builder.Property(p => p.Status).HasDefaultValue(PropertyStatus.Draft);
        builder.Property(p => p.Price).HasColumnType("numeric(18,2)");
        builder.Property(p => p.Size).HasColumnType("numeric(10,2)");

        builder.HasOne(p => p.Agent)
            .WithMany(tm => tm.Properties)
            .HasForeignKey(p => p.AgentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Translations)
            .WithOne(t => t.Property)
            .HasForeignKey(t => t.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Property)
            .HasForeignKey(i => i.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Features)
            .WithOne(f => f.Property)
            .HasForeignKey(f => f.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PropertyTranslationConfiguration : IEntityTypeConfiguration<PropertyTranslation>
{
    public void Configure(EntityTypeBuilder<PropertyTranslation> builder)
    {
        builder.HasIndex(pt => new { pt.PropertyId, pt.Language }).IsUnique();

        builder.Property(pt => pt.Title).IsRequired();
        builder.Property(pt => pt.Description).IsRequired();
        builder.Property(pt => pt.Address).IsRequired();
        builder.Property(pt => pt.City).IsRequired();
    }
}

public class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
{
    public void Configure(EntityTypeBuilder<PropertyImage> builder)
    {
        builder.Property(pi => pi.Url).IsRequired();
        builder.HasIndex(pi => new { pi.PropertyId, pi.SortOrder });
    }
}

public class PropertyFeatureConfiguration : IEntityTypeConfiguration<PropertyFeature>
{
    public void Configure(EntityTypeBuilder<PropertyFeature> builder)
    {
        builder.Property(pf => pf.Feature).IsRequired();
    }
}
