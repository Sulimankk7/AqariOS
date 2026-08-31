namespace PropertyOS.Api.Models.Leasing;

/// <summary>
/// Request model for creating a new tenant person record.
/// </summary>
public record CreateTenantRequest(
    string Name,
    string NationalId,
    string Phone,
    string Email,
    string? Occupation = null,
    string? Employer = null,
    string? PhoneCountryCode = "JO"
);
