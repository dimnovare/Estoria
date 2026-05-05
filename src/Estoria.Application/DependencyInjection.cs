using Estoria.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Estoria.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AuditService reads HttpContext for the caller IP — Application owns the
        // dependency since the audit emit happens in service code, not controllers.
        services.AddHttpContextAccessor();

        services.AddScoped<PropertyService>();
        services.AddScoped<BlogService>();
        services.AddScoped<TeamService>();
        services.AddScoped<OfferedServiceService>();
        services.AddScoped<PageContentService>();
        services.AddScoped<ContactService>();
        services.AddScoped<NewsletterService>();
        services.AddScoped<CareerService>();
        services.AddScoped<SiteSettingService>();
        services.AddScoped<PublicLookupService>();
        services.AddScoped<AuditService>();
        services.AddScoped<UserManagementService>();

        return services;
    }
}
