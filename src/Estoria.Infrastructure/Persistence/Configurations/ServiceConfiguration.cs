using Estoria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasIndex(s => s.Slug).IsUnique();

        builder.Property(s => s.Slug).IsRequired();
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasMany(s => s.Translations)
            .WithOne(t => t.Service)
            .HasForeignKey(t => t.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ServiceTranslationConfiguration : IEntityTypeConfiguration<ServiceTranslation>
{
    public void Configure(EntityTypeBuilder<ServiceTranslation> builder)
    {
        builder.HasIndex(t => new { t.ServiceId, t.Language }).IsUnique();

        builder.Property(t => t.Name).IsRequired();
        builder.Property(t => t.Description).IsRequired();
    }
}
