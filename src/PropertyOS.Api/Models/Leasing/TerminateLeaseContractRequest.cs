using System;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Api.Models.Leasing;

/// <summary>
/// Request model for terminating an active lease contract.
/// </summary>
public record TerminateLeaseContractRequest(
    TerminationType TerminationType,
    DateTime TerminationDate,
    decimal OutstandingBalance = 0,
    decimal DepositReturnedAmount = 0,
    decimal DepositDeductionAmount = 0,
    string? DepositDeductionReason = null,
    bool FinalUtilitySettlementCompleted = false,
    string? Reason = null,
    string? Notes = null
);
