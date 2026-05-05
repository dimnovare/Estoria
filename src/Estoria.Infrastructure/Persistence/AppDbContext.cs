using System.Reflection;
using Estoria.Application.Interfaces;
using Estoria.Domain.Base;
using Estoria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyTranslation> PropertyTranslations => Set<PropertyTranslation>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<PropertyFeature> PropertyFeatures => Set<PropertyFeature>();

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamMemberTranslation> TeamMemberTranslations => Set<TeamMemberTranslation>();

    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<BlogPostTranslation> BlogPostTranslations => Set<BlogPostTranslation>();

    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceTranslation> ServiceTranslations => Set<ServiceTranslation>();

    public DbSet<PageContent> PageContents => Set<PageContent>();
    public DbSet<PageContentTranslation> PageContentTranslations => Set<PageContentTranslation>();

    public DbSet<CareerPosting> CareerPostings => Set<CareerPosting>();
    public DbSet<CareerPostingTranslation> CareerPostingTranslations => Set<CareerPostingTranslation>();

    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRoleAssignment> UserRoles => Set<UserRoleAssignment>();

    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<DealParticipant> DealParticipants => Set<DealParticipant>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ContactNote> ContactNotes => Set<ContactNote>();

    public DbSet<AppTask> Tasks => Set<AppTask>();
    public DbSet<BirthdayTemplate> BirthdayTemplates => Set<BirthdayTemplate>();
    public DbSet<BirthdayTemplateTranslation> BirthdayTemplateTranslations => Set<BirthdayTemplateTranslation>();

    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
    public DbSet<PropertyEvent> PropertyEvents => Set<PropertyEvent>();

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return await base.SaveChangesAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
