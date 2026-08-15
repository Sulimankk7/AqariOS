using System.Text.Json.Serialization;

namespace PropertyOS.Application.Leasing.Commands.ProvisionTenantAccount;

/// <summary>
/// Specifies the primary contact method for provisioning a tenant account.
/// Supports both string ("Phone", "Email") and numeric (0, 1) JSON deserialization.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantProvisioningContactMethod
{
    Phone = 0,
    Email = 1
}
