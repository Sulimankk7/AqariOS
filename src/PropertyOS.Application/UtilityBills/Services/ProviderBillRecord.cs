using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Services;

/// <summary>
/// One bill record as returned by a utility billing provider.
/// This is a provider-neutral DTO — provider-specific fields must not
/// leak into domain entities or application handlers.
/// </summary>
/// <param name="ExternalId">
///   Stable provider-assigned bill identifier.
///   Used as provider_external_id in the uniqueness constraint.
///   Never logged in full; masked where needed.
/// </param>
/// <param name="BillDate">Billing date as confirmed by provider.</param>
/// <param name="DueDate">Payment due date, when available.</param>
/// <param name="Amount">Bill amount. Must be positive.</param>
/// <param name="Currency">3-character ISO currency code (e.g. "JOD").</param>
/// <param name="IsPaid">Whether the provider reports this bill as paid.</param>
/// <param name="PaymentStatus">More detailed payment status from provider.</param>
/// <param name="ProviderReference">
///   Raw provider reference string for traceability.
///   Stored but never logged in full.
/// </param>
public sealed record ProviderBillRecord(
    string ExternalId,
    DateOnly BillDate,
    DateOnly? DueDate,
    decimal Amount,
    string Currency,
    bool IsPaid,
    UtilityBillPaymentStatus PaymentStatus,
    string? ProviderReference);
