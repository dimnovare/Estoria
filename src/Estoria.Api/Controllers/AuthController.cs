using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Estoria.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config) => _config = config;

    public record LoginRequest(string Email, string Password);
    public record LoginResponse(string Token, string Email, DateTime ExpiresAt);

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var adminEmail = _config["Admin:Email"]
            ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var adminPassword = _config["Admin:Password"]
            ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            return StatusCode(500, new { error = "Admin credentials not configured" });

        if (!string.Equals(req.Email, adminEmail, StringComparison.OrdinalIgnoreCase) ||
            req.Password != adminPassword)
            return Unauthorized(new { error = "Invalid credentials" });

        var jwtSecret = _config["Jwt:Secret"]
            ?? Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? "dev-only-secret-change-in-production-this-must-be-32-chars-or-more";

        var expiresAt = DateTime.UtcNow.AddHours(8);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: new[]
            {
                new Claim(ClaimTypes.Email, adminEmail),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(JwtRegisteredClaimNames.Sub, adminEmail),
            },
            expires: expiresAt,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new LoginResponse(tokenString, adminEmail, expiresAt));
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Me()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        return Ok(new { email, role = "Admin" });
    }
}
