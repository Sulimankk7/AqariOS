namespace PropertyOS.Api.Models.Leasing;

/// <summary>
/// Request model for updating an existing tenant person record.
/// </summary>
public record UpdateTenantRequest(
    string Name,
    string NationalId,
    string Phone,
    string? Occupation = null,
    string? Employer = null
);
