using System;

namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Request model describing a single allocation line: an amount applied against an obligation payment.
/// </summary>
public record AllocationDetailRequest(
    Guid ObligationPaymentId,
    decimal Amount
);
