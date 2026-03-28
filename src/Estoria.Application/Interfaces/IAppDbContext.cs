using Estoria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Property> Properties { get; }
    DbSet<PropertyTranslation> PropertyTranslations { get; }
    DbSet<PropertyImage> PropertyImages { get; }
    DbSet<PropertyFeature> PropertyFeatures { get; }

    DbSet<TeamMember> TeamMembers { get; }
    DbSet<TeamMemberTranslation> TeamMemberTranslations { get; }

    DbSet<BlogPost> BlogPosts { get; }
    DbSet<BlogPostTranslation> BlogPostTranslations { get; }

    DbSet<Service> Services { get; }
    DbSet<ServiceTranslation> ServiceTranslations { get; }

    DbSet<PageContent> PageContents { get; }
    DbSet<PageContentTranslation> PageContentTranslations { get; }

    DbSet<CareerPosting> CareerPostings { get; }
    DbSet<CareerPostingTranslation> CareerPostingTranslations { get; }

    DbSet<NewsletterSubscriber> NewsletterSubscribers { get; }
    DbSet<ContactMessage> ContactMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
