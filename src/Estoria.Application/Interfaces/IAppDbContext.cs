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

    DbSet<SiteSetting> SiteSettings { get; }

    DbSet<User> Users { get; }
    DbSet<UserRoleAssignment> UserRoles { get; }

    DbSet<AuditLogEntry> AuditLog { get; }

    DbSet<Contact> Contacts { get; }
    DbSet<Deal> Deals { get; }
    DbSet<DealParticipant> DealParticipants { get; }
    DbSet<Activity> Activities { get; }
    DbSet<ContactNote> ContactNotes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
