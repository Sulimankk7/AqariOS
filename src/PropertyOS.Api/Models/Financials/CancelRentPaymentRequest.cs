namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Request model for cancelling a rent payment.
/// </summary>
public record CancelRentPaymentRequest(
    string? Reason = null
);
