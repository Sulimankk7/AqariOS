using PropertyOS.Application.Leasing.Commands.ProvisionTenantAccount;

namespace PropertyOS.Api.Models.Leasing;

/// <summary>
/// Payload for enabling Tenant Portal access for an existing Tenant record.
/// ContactMethod selects the delivery channel for the activation link only.
/// </summary>
public class ProvisionTenantAccountRequest
{
    /// <summary>
    /// Delivery channel for the activation link: Phone (default) or Email.
    /// Does NOT affect how the user identity is resolved.
    /// </summary>
    public TenantProvisioningContactMethod ContactMethod { get; set; } = TenantProvisioningContactMethod.Phone;

    /// <summary>
    /// Optional phone number override. If omitted, the tenant's existing phone is used.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email address for activation-link delivery. Required when ContactMethod is Email.
    /// </summary>
    public string? Email { get; set; }
}
