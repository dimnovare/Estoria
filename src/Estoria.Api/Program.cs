using System.Text.Json;
using System.Text.Json.Serialization;
using Estoria.Api.Middleware;
using Estoria.Application;
using Estoria.Infrastructure;
using Estoria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    var seeder = new DataSeeder(db);
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
