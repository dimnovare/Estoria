using Estoria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class MailboxLinkConfiguration : IEntityTypeConfiguration<MailboxLink>
{
    public void Configure(EntityTypeBuilder<MailboxLink> builder)
    {
        builder.Property(m => m.GraphMessageId).IsRequired().HasMaxLength(255);
        builder.Property(m => m.GraphConversationId).IsRequired().HasMaxLength(255);
        builder.Property(m => m.InternetMessageId).IsRequired().HasMaxLength(500);

        builder.Property(m => m.Subject).HasMaxLength(500);
        builder.Property(m => m.FromAddress).HasMaxLength(320);

        // Graph message ids are mailbox-scoped — unique per mailbox is fine
        // for our single-mailbox setup. InternetMessageId is globally unique
        // by RFC, so the unique constraint there guards against double-insert
        // when a sync overlaps a webhook.
        builder.HasIndex(m => m.GraphMessageId).IsUnique();
        builder.HasIndex(m => m.InternetMessageId).IsUnique();
        builder.HasIndex(m => m.GraphConversationId);

        // Inbox queries filter by linked-entity + chronology.
        builder.HasIndex(m => new { m.ContactId, m.ReceivedAt }).IsDescending(false, true);
        builder.HasIndex(m => new { m.DealId,    m.ReceivedAt }).IsDescending(false, true);
        builder.HasIndex(m => m.ReceivedAt).IsDescending();

        builder.HasOne(m => m.Contact)
            .WithMany()
            .HasForeignKey(m => m.ContactId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Deal)
            .WithMany()
            .HasForeignKey(m => m.DealId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Property)
            .WithMany()
            .HasForeignKey(m => m.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
