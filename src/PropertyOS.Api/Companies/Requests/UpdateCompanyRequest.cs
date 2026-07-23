namespace PropertyOS.Api.Companies.Requests;

/// <summary>
/// Request payload for updating basic company profile details.
/// </summary>
public record UpdateCompanyRequest(
    string LegalName,
    string DisplayName,
    string PrimaryPhone,
    string? PrimaryEmail
);
