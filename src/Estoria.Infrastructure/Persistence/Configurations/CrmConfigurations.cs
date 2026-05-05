using Estoria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.Property(c => c.FullName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).HasMaxLength(255);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.SecondaryPhone).HasMaxLength(50);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.Company).HasMaxLength(200);
        builder.Property(c => c.Position).HasMaxLength(200);

        builder.Property(c => c.Tags).HasColumnType("text[]");

        builder.HasOne(c => c.AssignedAgent)
            .WithMany()
            .HasForeignKey(c => c.AssignedAgentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Lookup paths: by email/phone (deduplication, search), by agent (caseload),
        // by birthday for the automation job (filtered on NOT NULL — most rows have
        // no DOB so the partial index is much smaller than a full one).
        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => c.Phone);
        builder.HasIndex(c => c.AssignedAgentId);
        builder.HasIndex(c => c.DateOfBirth)
            .HasFilter("\"DateOfBirth\" IS NOT NULL");
        builder.HasIndex(c => c.CreatedAt).IsDescending();
    }
}

public class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.Property(d => d.Title).IsRequired().HasMaxLength(300);
        builder.Property(d => d.Currency).IsRequired().HasMaxLength(10);

        // Decimal precision — store cents-level for prices and a 4-digit
        // commission percent (e.g. 2.5000).
        builder.Property(d => d.ExpectedValue).HasPrecision(18, 2);
        builder.Property(d => d.ActualValue).HasPrecision(18, 2);
        builder.Property(d => d.CommissionPercent).HasPrecision(6, 4);

        builder.HasOne(d => d.Property)
            .WithMany()
            .HasForeignKey(d => d.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.PrimaryContact)
            .WithMany()
            .HasForeignKey(d => d.PrimaryContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.AssignedAgent)
            .WithMany()
            .HasForeignKey(d => d.AssignedAgentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Participants)
            .WithOne(p => p.Deal)
            .HasForeignKey(p => p.DealId)
            .OnDelete(DeleteBehavior.Cascade);

        // Kanban board groups by stage; agent dashboards filter by AssignedAgentId;
        // contact/property pages list deals via PrimaryContactId/PropertyId;
        // recent-activity views sort by StageChangedAt DESC.
        builder.HasIndex(d => d.Stage);
        builder.HasIndex(d => d.AssignedAgentId);
        builder.HasIndex(d => d.PrimaryContactId);
        builder.HasIndex(d => d.PropertyId);
        builder.HasIndex(d => d.StageChangedAt).IsDescending();
    }
}

public class DealParticipantConfiguration : IEntityTypeConfiguration<DealParticipant>
{
    public void Configure(EntityTypeBuilder<DealParticipant> builder)
    {
        builder.Property(p => p.Role).IsRequired().HasMaxLength(50);

        builder.HasOne(p => p.Contact)
            .WithMany()
            .HasForeignKey(p => p.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        // Same contact can play one role per deal (e.g. only one "lawyer").
        builder.HasIndex(p => new { p.DealId, p.ContactId, p.Role }).IsUnique();
    }
}

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.Property(a => a.Title).IsRequired().HasMaxLength(300);

        builder.HasOne(a => a.Deal)
            .WithMany()
            .HasForeignKey(a => a.DealId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Contact)
            .WithMany()
            .HasForeignKey(a => a.ContactId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Property)
            .WithMany()
            .HasForeignKey(a => a.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Timeline reads: "show me everything on this contact/deal newest-first".
        builder.HasIndex(a => new { a.ContactId, a.OccurredAt }).IsDescending(false, true);
        builder.HasIndex(a => new { a.DealId,    a.OccurredAt }).IsDescending(false, true);
        builder.HasIndex(a => a.UserId);
    }
}

public class ContactNoteConfiguration : IEntityTypeConfiguration<ContactNote>
{
    public void Configure(EntityTypeBuilder<ContactNote> builder)
    {
        builder.Property(n => n.Body).IsRequired();

        builder.HasOne(n => n.Contact)
            .WithMany()
            .HasForeignKey(n => n.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => n.ContactId);
    }
}
