namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Optional request body for operator-initiated cancellation of an eFAWATEERcom transaction.
/// </summary>
public record CancelEfawateercomTransactionRequest(
    string? Reason = null
);
