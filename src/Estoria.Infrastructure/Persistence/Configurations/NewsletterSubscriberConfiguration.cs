using Estoria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        builder.HasIndex(ns => ns.Email).IsUnique();

        builder.Property(ns => ns.Email).IsRequired();
        builder.Property(ns => ns.IsActive).HasDefaultValue(true);

        // Default to empty so the schema is consistent with legacy rows that
        // get backfilled by the AddNewsletterUnsubscribeToken migration.
        builder.Property(ns => ns.UnsubscribeToken)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue(string.Empty);

        // Token-based unsubscribe must be O(1) and unique. The unique guard
        // catches accidental duplicate generation; conflict surface is tiny
        // (32-byte random token).
        builder.HasIndex(ns => ns.UnsubscribeToken).IsUnique();
    }
}

public class NewsletterCampaignConfiguration : IEntityTypeConfiguration<NewsletterCampaign>
{
    public void Configure(EntityTypeBuilder<NewsletterCampaign> builder)
    {
        builder.Property(c => c.Subject).IsRequired().HasMaxLength(300);
        builder.Property(c => c.BodyHtml).IsRequired().HasColumnType("text");

        // Admin list reads chronologically, status filter narrows further.
        builder.HasIndex(c => c.CreatedAt).IsDescending();
        builder.HasIndex(c => c.Status);
    }
}
