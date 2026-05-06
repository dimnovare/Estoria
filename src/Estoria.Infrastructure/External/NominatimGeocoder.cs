using System.Globalization;
using System.Text.Json;
using Estoria.Application.DTOs.Properties;
using Estoria.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Estoria.Infrastructure.External;

/// <summary>
/// Forward-geocodes addresses against OpenStreetMap Nominatim. Free, no API
/// key, accurate enough for property-level precision in Estonia.
/// Nominatim's usage policy demands:
///   * a descriptive User-Agent (set on the named HttpClient at registration),
///   * at most 1 request per second per IP.
/// We enforce the rate limit here with a process-wide SemaphoreSlim + a
/// minimum-gap stopwatch so two concurrent admin clicks can't burst.
/// </summary>
public class NominatimGeocoder : IGeocoder
{
    private const string SearchEndpoint =
        "https://nominatim.openstreetmap.org/search?format=json&limit=1&countrycodes=ee&q=";

    // Process-wide gate. Max 1 in flight at a time + a 1-second floor between
    // releases so we stay well clear of the policy threshold even under load.
    private static readonly SemaphoreSlim _gate = new(1, 1);
    private static DateTime _lastCallUtc = DateTime.MinValue;

    private static readonly TimeSpan MinGap = TimeSpan.FromSeconds(1);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly ILogger<NominatimGeocoder> _logger;

    public NominatimGeocoder(HttpClient http, ILogger<NominatimGeocoder> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<GeoCoordsDto?> GeocodeAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        await _gate.WaitAsync(ct);
        try
        {
            // Sleep out the remainder of the minimum 1-second window since
            // the previous call. First-ever call falls through immediately.
            var elapsed = DateTime.UtcNow - _lastCallUtc;
            if (elapsed < MinGap)
                await Task.Delay(MinGap - elapsed, ct);

            var url = SearchEndpoint + Uri.EscapeDataString(query);
            using var resp = await _http.GetAsync(url, ct);
            _lastCallUtc = DateTime.UtcNow;

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "NOMINATIM_FAIL status={Status} q={Query}",
                    (int)resp.StatusCode, query);
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            var arr  = JsonSerializer.Deserialize<List<NominatimResult>>(json, JsonOpts);
            var first = arr?.FirstOrDefault();
            if (first is null) return null;

            // Nominatim returns lat/lon as strings; parse with invariant
            // culture so a comma-decimal locale on the host doesn't break us.
            if (!double.TryParse(first.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                !double.TryParse(first.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            {
                _logger.LogWarning("NOMINATIM_PARSE_FAIL lat={Lat} lon={Lon} q={Query}",
                    first.Lat, first.Lon, query);
                return null;
            }

            return new GeoCoordsDto { Latitude = lat, Longitude = lon };
        }
        finally
        {
            _gate.Release();
        }
    }

    private class NominatimResult
    {
        public string Lat { get; set; } = string.Empty;
        public string Lon { get; set; } = string.Empty;
    }
}
