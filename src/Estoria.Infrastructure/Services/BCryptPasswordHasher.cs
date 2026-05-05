using Estoria.Application.Interfaces;

namespace Estoria.Infrastructure.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
    // Work factor 12 — ~250ms on modern hardware. Bump to 13/14 if/when login
    // throughput stops being interactive on the production box.
    private const int WorkFactor = 12;

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash was malformed — treat as a failed verify rather than a 500.
            return false;
        }
    }
}
