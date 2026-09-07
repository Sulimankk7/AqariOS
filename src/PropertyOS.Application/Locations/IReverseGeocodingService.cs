namespace PropertyOS.Application.Locations;

public interface IReverseGeocodingService
{
    Task<ReverseGeocodingResult?> ReverseAsync(decimal latitude, decimal longitude, string language, CancellationToken cancellationToken = default);
}

public sealed record ReverseGeocodingResult(
    string? CountryCode, string? Country, string? Governorate, string? City,
    string? District, string? Neighborhood, string? Street, string? HouseNumber,
    string? PostalCode, string? FormattedAddress, string Language);
