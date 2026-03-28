using Estoria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class CareerPostingConfiguration : IEntityTypeConfiguration<CareerPosting>
{
    public void Configure(EntityTypeBuilder<CareerPosting> builder)
    {
        builder.HasIndex(cp => cp.Slug).IsUnique();

        builder.Property(cp => cp.Slug).IsRequired();
        builder.Property(cp => cp.IsActive).HasDefaultValue(true);

        builder.HasMany(cp => cp.Translations)
            .WithOne(t => t.CareerPosting)
            .HasForeignKey(t => t.CareerPostingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CareerPostingTranslationConfiguration : IEntityTypeConfiguration<CareerPostingTranslation>
{
    public void Configure(EntityTypeBuilder<CareerPostingTranslation> builder)
    {
        builder.HasIndex(t => new { t.CareerPostingId, t.Language }).IsUnique();

        builder.Property(t => t.Title).IsRequired();
        builder.Property(t => t.Description).IsRequired();
    }
}
