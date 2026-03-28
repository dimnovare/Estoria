using Estoria.Application.Interfaces;
using Estoria.Infrastructure.Persistence;
using Estoria.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Estoria.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config,
        bool isDevelopment,
        string? webRootPath = null)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Dev (webRootPath provided) → local disk; Prod → Cloudflare R2
        if (webRootPath is not null)
        {
            services.AddSingleton<IFileStorageService>(
                new LocalFileStorageService(webRootPath));
        }
        else
        {
            services.AddScoped<IFileStorageService, R2FileStorageService>();
        }

        // Email: dev → console logger; prod → Resend HTTP API
        services.AddHttpClient("Resend");

        if (isDevelopment)
            services.AddScoped<IEmailService, ConsoleEmailService>();
        else
            services.AddScoped<IEmailService, ResendEmailService>();

        return services;
    }
}
