using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Estoria.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;

    public AuthController(IConfiguration config, IAppDbContext db, IPasswordHasher hasher)
    {
        _config = config;
        _db     = db;
        _hasher = hasher;
    }

    public record LoginRequest(string Email, string Password);

    public class UserSummaryDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string[] Roles { get; set; } = [];
        public string? PhotoUrl { get; set; }
        public string[] Languages { get; set; } = [];
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserSummaryDto User { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest req,
        CancellationToken ct = default)
    {
        // Case-insensitive email lookup. EF translates ToLower() to LOWER() in SQL.
        var emailNormalized = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized, ct);

        if (user is null || !user.IsActive || !_hasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid credentials" });

        // Touch LastLoginAt before issuing the token. AppDbContext.SaveChangesAsync
        // also updates UpdatedAt automatically.
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var roles = user.RoleAssignments.Select(a => a.Role.ToString()).ToArray();
        var (token, expiresAt) = IssueToken(user, roles);

        return Ok(new LoginResponse
        {
            Token     = token,
            ExpiresAt = expiresAt,
            User      = ToSummary(user, roles),
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct = default)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var user = await _db.Users
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null || !user.IsActive)
            return Unauthorized();

        var roles = user.RoleAssignments.Select(a => a.Role.ToString()).ToArray();
        return Ok(ToSummary(user, roles));
    }

    private (string token, DateTime expiresAt) IssueToken(User user, string[] roles)
    {
        var jwtSecret = _config["Jwt:Secret"]
            ?? Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? "dev-only-secret-change-in-production-this-must-be-32-chars-or-more";

        var expiresAt = DateTime.UtcNow.AddHours(8);
        var key       = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds     = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier,   user.Id.ToString()),
            new(ClaimTypes.Email,            user.Email),
            new(ClaimTypes.Name,             user.FullName),
        };

        // One Role claim per assignment — multi-role-aware.
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static UserSummaryDto ToSummary(User user, string[] roles) => new()
    {
        Id        = user.Id,
        Email     = user.Email,
        FullName  = user.FullName,
        Roles     = roles,
        PhotoUrl  = user.PhotoUrl,
        Languages = [.. user.Languages],
    };
}
