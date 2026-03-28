using Estoria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.HasIndex(tm => tm.Slug).IsUnique();

        builder.Property(tm => tm.Slug).IsRequired();
        builder.Property(tm => tm.Phone).IsRequired();
        builder.Property(tm => tm.Email).IsRequired();
        builder.Property(tm => tm.IsActive).HasDefaultValue(true);
        builder.Property(tm => tm.Languages).HasColumnType("text[]");

        builder.HasMany(tm => tm.Translations)
            .WithOne(t => t.TeamMember)
            .HasForeignKey(t => t.TeamMemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TeamMemberTranslationConfiguration : IEntityTypeConfiguration<TeamMemberTranslation>
{
    public void Configure(EntityTypeBuilder<TeamMemberTranslation> builder)
    {
        builder.HasIndex(t => new { t.TeamMemberId, t.Language }).IsUnique();

        builder.Property(t => t.Name).IsRequired();
        builder.Property(t => t.Role).IsRequired();
    }
}
