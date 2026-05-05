using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class AppTaskConfiguration : IEntityTypeConfiguration<AppTask>
{
    public void Configure(EntityTypeBuilder<AppTask> builder)
    {
        builder.Property(t => t.Title).IsRequired().HasMaxLength(300);
        builder.Property(t => t.ReminderJobId).HasMaxLength(100);
        builder.Property(t => t.Recurrence).HasMaxLength(200);

        // Mine view: "all open tasks assigned to me, soonest first" hits this
        // composite index. Status is part of the key so the query stays selective
        // even after thousands of completed rows pile up.
        builder.HasIndex(t => new { t.AssignedToUserId, t.Status });

        // Daily reminder sweep / overdue dashboard. Partial index on Pending
        // keeps the index small — done/cancelled rows are dead weight here.
        builder.HasIndex(t => t.DueAt)
            .HasFilter($"\"Status\" = {(int)AppTaskStatus.Pending}");

        builder.HasIndex(t => t.ContactId);
        builder.HasIndex(t => t.DealId);
    }
}

public class BirthdayTemplateConfiguration : IEntityTypeConfiguration<BirthdayTemplate>
{
    public void Configure(EntityTypeBuilder<BirthdayTemplate> builder)
    {
        builder.HasMany(t => t.Translations)
            .WithOne(tr => tr.BirthdayTemplate)
            .HasForeignKey(tr => tr.BirthdayTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BirthdayTemplateTranslationConfiguration
    : IEntityTypeConfiguration<BirthdayTemplateTranslation>
{
    public void Configure(EntityTypeBuilder<BirthdayTemplateTranslation> builder)
    {
        builder.Property(t => t.Subject).IsRequired().HasMaxLength(300);
        builder.Property(t => t.BodyHtml).IsRequired();

        // One translation per (template, language). Birthday is currently a
        // singleton template, but the unique key keeps the schema honest if we
        // ever support multiple campaigns.
        builder.HasIndex(t => new { t.BirthdayTemplateId, t.Language }).IsUnique();
    }
}
