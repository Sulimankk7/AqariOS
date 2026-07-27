using System;

namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Request model for registering an outbound eFAWATEERcom transaction against a rent payment.
/// </summary>
public record CreateEfawateercomTransactionRequest(
    Guid RentPaymentId,
    string ExternalTransactionId,
    decimal Amount,
    string? PaymentReference = null
);
