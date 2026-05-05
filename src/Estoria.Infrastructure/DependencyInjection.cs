using Estoria.Application.Interfaces;
using Estoria.Infrastructure.Jobs;
using Estoria.Infrastructure.Persistence;
using Estoria.Infrastructure.Services;
using Hangfire;
using Hangfire.PostgreSql;
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

        // Password hashing — bcrypt, work factor 12 (see BCryptPasswordHasher)
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        // Reminder job concrete implementation; the interface lives in Application so
        // TaskService can schedule jobs without a Hangfire-server reference.
        services.AddScoped<IReminderJobService, ReminderJobService>();

        services.AddJobInfrastructure(config);

        return services;
    }

    /// <summary>
    /// Wires Hangfire on top of the existing Postgres connection. Hangfire creates
    /// its own schema ("hangfire") on first run and is happy to share the database
    /// with the application — Postgres handles the locking so the worker pool can
    /// scale horizontally if we ever move beyond one instance.
    /// </summary>
    public static IServiceCollection AddJobInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be set before Hangfire can start.");

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire",
                    PrepareSchemaIfNecessary = true,
                }));

        // Two workers is enough for reminder + birthday traffic. Queues let us
        // segregate slow lanes (email blast) from fast ones (reminder fires) when
        // we add more job kinds later.
        services.AddHangfireServer(o =>
        {
            o.WorkerCount = 2;
            o.Queues = new[] { "default", "email", "media" };
        });

        return services;
    }
}
