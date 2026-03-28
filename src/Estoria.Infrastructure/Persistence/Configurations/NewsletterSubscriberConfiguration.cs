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
    }
}
