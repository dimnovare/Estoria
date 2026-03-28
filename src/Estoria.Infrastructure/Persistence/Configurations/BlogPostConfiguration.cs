using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Estoria.Infrastructure.Persistence.Configurations;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.HasIndex(bp => bp.Slug).IsUnique();
        builder.HasIndex(bp => bp.Status);

        builder.Property(bp => bp.Slug).IsRequired();
        builder.Property(bp => bp.Status).HasDefaultValue(BlogPostStatus.Draft);

        builder.HasOne(bp => bp.Author)
            .WithMany(tm => tm.BlogPosts)
            .HasForeignKey(bp => bp.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(bp => bp.Translations)
            .WithOne(t => t.BlogPost)
            .HasForeignKey(t => t.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BlogPostTranslationConfiguration : IEntityTypeConfiguration<BlogPostTranslation>
{
    public void Configure(EntityTypeBuilder<BlogPostTranslation> builder)
    {
        builder.HasIndex(t => new { t.BlogPostId, t.Language }).IsUnique();

        builder.Property(t => t.Title).IsRequired();
        builder.Property(t => t.Content).IsRequired();
    }
}
