using Estoria.Application.DTOs.Properties;

namespace Estoria.Application.Interfaces;

/// <summary>
/// Resolves a free-text address to lat/long. Implementations are expected to
/// honor whatever rate-limit policy the upstream provider mandates — callers
/// should treat the operation as potentially blocking for a second or two.
/// </summary>
public interface IGeocoder
{
    /// <summary>Returns null when the upstream returns no result.</summary>
    Task<GeoCoordsDto?> GeocodeAsync(string query, CancellationToken ct = default);
}
