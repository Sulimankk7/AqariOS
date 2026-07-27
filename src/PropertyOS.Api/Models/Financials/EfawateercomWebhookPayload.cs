using System;

namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// PROVISIONAL webhook payload shape for inbound eFAWATEERcom callbacks, pending the real
/// gateway specification. Deserialized case-insensitively from the raw (signature-verified)
/// request body. <c>Status</c> is the gateway status as a string and is mapped to
/// <see cref="PropertyOS.Domain.Financials.Enums.EfawateercomStatus"/> by name (case-insensitive).
/// </summary>
public record EfawateercomWebhookPayload(
    string? ExternalTransactionId,
    string? Status,
    DateTimeOffset? ResponseTime = null,
    string? ResponseCode = null,
    string? ResponseMessage = null
);
