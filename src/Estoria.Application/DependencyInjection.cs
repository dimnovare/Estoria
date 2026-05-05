using Estoria.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Estoria.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PropertyService>();
        services.AddScoped<BlogService>();
        services.AddScoped<TeamService>();
        services.AddScoped<OfferedServiceService>();
        services.AddScoped<PageContentService>();
        services.AddScoped<ContactService>();
        services.AddScoped<NewsletterService>();
        services.AddScoped<CareerService>();
        services.AddScoped<SiteSettingService>();

        return services;
    }
}
