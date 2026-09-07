namespace PropertyOS.Application.Locations;

public sealed class GeocodingOptions
{
    public const string SectionName = "Geocoding";
    public string Provider { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.geoapify.com/";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 8;
    public int CacheDurationSeconds { get; set; } = 900;
}
