using PropertyOS.Domain.Companies.Enums;

namespace PropertyOS.Application.DTOs.Identity;

/// <summary>
/// Request payload for user registration and tenant company onboarding.
/// </summary>
public class RegisterRequestDto
{
    /// <summary>
    /// User's full name. Required.
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// User's email address. Required if phone is omitted.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User's primary phone number. Required if email is omitted.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Plain-text user password. Required.
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// Legal registered name of the company / organization. Required.
    /// </summary>
    public string CompanyName { get; set; } = null!;

    /// <summary>
    /// Display or trading name of the company. Optional (defaults to Legal Name).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Classification type of the company. Default: IndividualOwner.
    /// </summary>
    public CompanyType CompanyType { get; set; } = CompanyType.IndividualOwner;

    /// <summary>
    /// ISO 3166-1 alpha-2 country code. Default: "JO".
    /// </summary>
    public string CountryCode { get; set; } = "JO";

    /// <summary>
    /// Preferred UI language for user profile ("ar" or "en"). Default: "ar".
    /// </summary>
    public string PreferredLanguage { get; set; } = "ar";
}
