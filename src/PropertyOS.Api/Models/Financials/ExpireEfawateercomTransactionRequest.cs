namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Optional request body for marking a stale eFAWATEERcom transaction as timed out.
/// </summary>
public record ExpireEfawateercomTransactionRequest(
    string? ResponseCode = null,
    string? ResponseMessage = null
);
