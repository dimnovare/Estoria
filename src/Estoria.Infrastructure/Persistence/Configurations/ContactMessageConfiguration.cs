using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.Property(cm => cm.Name).IsRequired();
        builder.Property(cm => cm.Email).IsRequired();
        builder.Property(cm => cm.Message).IsRequired();
        builder.Property(cm => cm.Status).HasDefaultValue(ContactStatus.New);

        builder.HasOne(cm => cm.Property)
            .WithMany()
            .HasForeignKey(cm => cm.PropertyId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
