using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Locations;

namespace PropertyOS.Infrastructure.Locations;

public sealed class GeoapifyReverseGeocodingService : IReverseGeocodingService
{
    private readonly HttpClient _client;
    private readonly GeocodingOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeoapifyReverseGeocodingService> _logger;

    public GeoapifyReverseGeocodingService(HttpClient client, IOptions<GeocodingOptions> options, IMemoryCache cache, ILogger<GeoapifyReverseGeocodingService> logger)
    { _client = client; _options = options.Value; _cache = cache; _logger = logger; }

    public async Task<ReverseGeocodingResult?> ReverseAsync(decimal latitude, decimal longitude, string language, CancellationToken cancellationToken = default)
    {
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
        var lang = string.Equals(language, "ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";
        if (!string.Equals(_options.Provider, "Geoapify", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(_options.ApiKey))
            return null;

        var key = $"geocode:{decimal.Round(latitude, 6)}:{decimal.Round(longitude, 6)}:{lang}";
        if (_cache.TryGetValue(key, out ReverseGeocodingResult? cached)) return cached;

        var url = $"v1/geocode/reverse?lat={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lang={lang}&format=json&apiKey={Uri.EscapeDataString(_options.ApiKey.Trim())}";
        try
        {
            using var response = await _client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Reverse geocoding provider rejected request. StatusCode={StatusCode}", (int)response.StatusCode);
                return null;
            }
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!json.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0) return null;
            var p = results[0];
            if (p.ValueKind != JsonValueKind.Object) return null;
            var result = new ReverseGeocodingResult(S(p,"country_code",8)?.ToUpperInvariant(), S(p,"country",150), S(p,"state",150), S(p,"city",150), S(p,"county",150) ?? S(p,"district",150), S(p,"suburb",150) ?? S(p,"neighbourhood",150), S(p,"street",255), S(p,"housenumber",50), S(p,"postcode",20), S(p,"formatted",500), lang);
            _cache.Set(key, result, TimeSpan.FromSeconds(_options.CacheDurationSeconds));
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { _logger.LogWarning("Reverse geocoding request timed out."); return null; }
        catch (HttpRequestException ex) { _logger.LogWarning("Reverse geocoding network failure. ExceptionType={ExceptionType}", ex.GetType().Name); return null; }
        catch (JsonException) { _logger.LogWarning("Reverse geocoding provider returned malformed data."); return null; }
    }

    private static string? S(JsonElement e, string name, int max)
    {
        if (!e.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.String) return null;
        var raw = v.GetString()?.Trim();
        var s = raw is null ? null : new string(raw.Where(c => !char.IsControl(c)).ToArray());
        if (string.IsNullOrEmpty(s)) return null;
        return s.Length <= max ? s : s[..max];
    }
}
