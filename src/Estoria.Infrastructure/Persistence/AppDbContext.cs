using System.Reflection;
using Estoria.Application.Interfaces;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
