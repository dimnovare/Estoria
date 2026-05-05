using Estoria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.Property(s => s.Email).IsRequired().HasMaxLength(255);
        builder.Property(s => s.Name).HasMaxLength(200);
        builder.Property(s => s.UnsubscribeToken).IsRequired().HasMaxLength(64);
        builder.Property(s => s.FilterJson).IsRequired().HasColumnType("text");

        builder.HasOne(s => s.Contact)
            .WithMany()
            .HasForeignKey(s => s.ContactId)
            .OnDelete(DeleteBehavior.SetNull);

        // Lookup paths: by email (resubscribe / unsubscribe-by-email),
        // by contact (admin "search subscriptions for this contact"),
        // and the digest worker's "(active, frequency)" sweep.
        builder.HasIndex(s => s.Email);
        builder.HasIndex(s => s.ContactId);
        builder.HasIndex(s => new { s.IsActive, s.Frequency });

        // Token-based unsubscribe must be O(1) and unique. Conflict surface is
        // tiny (32-byte random) but the constraint guards against accidental
        // duplicate generation.
        builder.HasIndex(s => s.UnsubscribeToken).IsUnique();
    }
}

public class PropertyEventConfiguration : IEntityTypeConfiguration<PropertyEvent>
{
    public void Configure(EntityTypeBuilder<PropertyEvent> builder)
    {
        builder.Property(e => e.PreviousJson).HasColumnType("text");
        builder.Property(e => e.NewJson).HasColumnType("text");

        builder.HasOne(e => e.Property)
            .WithMany()
            .HasForeignKey(e => e.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Public history widget: "show me the last N events for this property,
        // newest first". Composite index avoids a separate sort step.
        builder.HasIndex(e => new { e.PropertyId, e.CreatedAt })
            .IsDescending(false, true);
    }
}
