using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Leasing.Commands.TerminateLeaseContract;

public record TerminateLeaseContractCommand(
    Guid ContractId,
    TerminationType TerminationType,
    DateTime TerminationDate,
    decimal OutstandingBalance = 0,
    decimal DepositReturnedAmount = 0,
    decimal DepositDeductionAmount = 0,
    string? DepositDeductionReason = null,
    bool FinalUtilitySettlementCompleted = false,
    string? Reason = null,
    string? Notes = null
) : ICommand;
