using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.ReversePaymentAllocation;

public record ReversePaymentAllocationCommand(
    Guid AllocationId,
    string ReversalReason,
    string? Notes = null) : ICommand;
