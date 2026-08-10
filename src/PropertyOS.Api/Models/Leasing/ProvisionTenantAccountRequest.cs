namespace PropertyOS.Api.Models.Leasing;

/// <summary>
/// Payload parameters for provisioning a User identity account for an existing Tenant person record.
/// </summary>
public class ProvisionTenantAccountRequest
{
    /// <summary>
    /// Optional email address for the tenant user account if not already provided on the tenant aggregate.
    /// </summary>
    public string? Email { get; set; }
}
