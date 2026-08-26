using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Api.UtilityBills;

/// <summary>Request model for linking a utility account to a lease contract.</summary>
public sealed record LinkUtilityAccountRequest(
    Guid LeaseContractId,
    UtilityType UtilityType,
    string AccountNumber,
    string? MeterNumber = null);
