using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Estoria.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Estoria.Application;
using Estoria.Infrastructure;
using Estoria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Railway provides DATABASE_URL as a postgres:// URI; convert to Npgsql
// connection string format if present. Falls back to appsettings
// ConnectionStrings:DefaultConnection for local dev.
var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(dbUrl))
{
    try
    {
        var uri = new Uri(dbUrl);
        var userInfo = uri.UserInfo.Split(':');
        var connStr = $"Host={uri.Host};Port={uri.Port};" +
                      $"Database={uri.AbsolutePath.TrimStart('/')};" +
                      $"Username={userInfo[0]};Password={userInfo[1]};" +
                      $"SSL Mode=Require;Trust Server Certificate=true";
        builder.Configuration["ConnectionStrings:DefaultConnection"] = connStr;
        Console.WriteLine("[Estoria] Using DATABASE_URL from environment");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Estoria] Failed to parse DATABASE_URL: {ex.Message}");
    }
}

// R2 storage env-var overrides — same pattern as DATABASE_URL.
foreach (var (envVar, configKey) in new[]
{
    ("R2_PUBLIC_BUCKET",     "Storage:PublicBucket"),
    ("R2_PRIVATE_BUCKET",    "Storage:PrivateBucket"),
    ("R2_PUBLIC_CDN_URL",    "Storage:PublicCdnUrl"),
    ("R2_ACCOUNT_ID",        "Storage:AccountId"),
    ("R2_ACCESS_KEY_ID",     "Storage:AccessKeyId"),
    ("R2_SECRET_ACCESS_KEY", "Storage:SecretAccessKey"),
})
{
    var value = Environment.GetEnvironmentVariable(envVar);
    if (!string.IsNullOrEmpty(value))
        builder.Configuration[configKey] = value;
}

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"];

builder.Services.AddCors(o => o.AddDefaultPolicy(b =>
{
    if (allowedOrigins.Contains("*"))
        b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    else
        b.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader()
         .AllowCredentials();
}));

// Allow multipart uploads up to 10 MB
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("contact", o =>
    {
        o.PermitLimit = 5;
        o.Window      = TimeSpan.FromHours(1);
    });
    options.AddFixedWindowLimiter("newsletter", o =>
    {
        o.PermitLimit = 3;
        o.Window      = TimeSpan.FromHours(1);
    });
    options.RejectionStatusCode = 429;
});

builder.Services.AddHealthChecks();

// ── JWT Authentication ───────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? "dev-only-secret-change-in-production-this-must-be-32-chars-or-more";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddApplication();

var isDev       = builder.Environment.IsDevelopment();
var webRootPath = isDev
    ? builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot")
    : null;

builder.Services.AddInfrastructure(builder.Configuration, isDev, webRootPath);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<LanguageMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
}

// Apply migrations synchronously with timeout — do NOT crash the app if it fails
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    await db.Database.MigrateAsync(cts.Token);
    Console.WriteLine("[Estoria] Migrations applied successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"[Estoria] Migration failed: {ex.Message}");
    // Continue anyway — let healthcheck pass and surface the issue via logs
}

// Seed in background — never block startup or healthcheck
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(3000);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new DataSeeder(db);
        await seeder.SeedAsync();
        Console.WriteLine("[Estoria] Background seeding completed");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Estoria] Background seeding failed: {ex.Message}");
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Simple health endpoint for Railway — never touches the database
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
