using Estoria.Application.Interfaces;
using Estoria.Infrastructure.External;
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

        // Dev (webRootPath provided) → local disk; Prod → Cloudflare R2.
        // A startup log line tells operators exactly which storage is active,
        // and a clear warning fires when the R2 keys are missing so the problem
        // shows up in deploy logs rather than as a cryptic 500 on first upload.
        var r2AccountId = config["Storage:AccountId"];
        var r2AccessKey = config["Storage:AccessKeyId"];
        var r2Secret    = config["Storage:SecretAccessKey"];
        var r2Public    = config["Storage:PublicBucket"];
        var r2Private   = config["Storage:PrivateBucket"];

        if (webRootPath is not null)
        {
            services.AddSingleton<IFileStorageService>(
                new LocalFileStorageService(webRootPath, config));
            Console.WriteLine("[Estoria] Storage: using LOCAL file storage (dev mode)");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(r2AccountId) ||
                string.IsNullOrWhiteSpace(r2AccessKey)  ||
                string.IsNullOrWhiteSpace(r2Secret))
            {
                Console.Error.WriteLine(
                    "[Estoria] ⚠ Storage: R2 credentials not configured — " +
                    "image uploads will fail. Set Storage:AccountId, " +
                    "Storage:AccessKeyId and Storage:SecretAccessKey.");
            }
            else
            {
                Console.WriteLine(
                    $"[Estoria] Storage: using R2 bucket '{r2Public}' / '{r2Private}'");
            }

            // R2FileStorageService holds an AmazonS3Client which is thread-safe
            // and expensive to construct; singleton is correct here.
            services.AddSingleton<IFileStorageService, R2FileStorageService>();
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

        // Image pipeline. Magick.NET instances are not thread-safe, so the
        // service is registered Scoped — a single Hangfire run owns its own
        // MagickImage objects for the lifetime of the request.
        services.AddScoped<IImageProcessingService, MagickImageProcessingService>();
        services.AddScoped<IImageProcessingJob, ImageProcessingJob>();

        // Graph is optional in dev. If TenantId is empty/whitespace, register a
        // NoOp service so the inbox controller can still activate and return
        // empty pages cleanly. Production sets all four Graph:* values via Railway env.
        var graphTenantId = config["Graph:TenantId"];
        if (!string.IsNullOrWhiteSpace(graphTenantId))
        {
            services.AddSingleton<IMailboxService, GraphMailService>();
        }
        else
        {
            services.AddSingleton<IMailboxService, NoOpMailboxService>();
        }

        // Nominatim geocoder. Typed HttpClient with the UA Nominatim's policy
        // requires; the geocoder itself enforces the 1 req/s rate limit.
        services.AddHttpClient<IGeocoder, NominatimGeocoder>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Estoria/1.0 (info@estoria.estate)");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

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
