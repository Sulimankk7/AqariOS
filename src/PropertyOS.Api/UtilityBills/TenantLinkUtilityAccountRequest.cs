using System.Text.Json.Serialization;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Api.UtilityBills;

/// <summary>
/// Tenant self-service request for linking a utility account.
/// Tenant, company, lease, and property identity are resolved from authenticated context.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TenantLinkUtilityAccountRequest(
    UtilityType UtilityType,
    string AccountNumber,
    string? MeterNumber = null);
