namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Request model for reversing an active payment allocation.
/// </summary>
public record ReversePaymentAllocationRequest(
    string ReversalReason,
    string? Notes = null
);
