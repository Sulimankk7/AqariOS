namespace PropertyOS.Api.Models.Leasing;

/// <summary>
/// Request model for updating an existing tenant person record.
/// </summary>
public record UpdateTenantRequest(
    string Name,
    string NationalId,
    string Phone,
    string? Email = null,
    string? Occupation = null,
    string? Employer = null
);
