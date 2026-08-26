using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Domain.UtilityBills;

/// <summary>
/// Represents one provider-confirmed utility bill.
///
/// Design rules:
///   - Append-only from the provider's perspective; no provider-facing mutation methods.
///   - The only mutable field is notification_sent_at (idempotency gate).
///   - Database-level uniqueness (utility_account_id, provider_external_id) prevents
///     duplicate inserts. The repository uses ON CONFLICT DO NOTHING.
///   - is_from_historical_backfill = true suppresses all notification logic for
///     bills imported during the initial bootstrap. Never notify on historical bills.
/// </summary>
public sealed class UtilityBill
{
    // ── EF Core parameterless constructor ───────────────────────────────────
    private UtilityBill() { }

    // ── Identity ────────────────────────────────────────────────────────────
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid UtilityAccountId { get; private set; }
    public UtilityType UtilityType { get; private set; }

    // ── Provider identity ────────────────────────────────────────────────────
    /// <summary>
    /// Stable provider-assigned bill identifier.
    /// Together with UtilityAccountId, forms the UNIQUE constraint that prevents
    /// duplicate bill records. Never logged in full; masked where needed.
    /// </summary>
    public string ProviderExternalId { get; private set; } = string.Empty;

    // ── Bill content ─────────────────────────────────────────────────────────
    public DateOnly BillDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "JOD";
    public bool IsPaid { get; private set; }
    public UtilityBillPaymentStatus PaymentStatus { get; private set; }

    /// <summary>
    /// Raw provider reference string. Stored for traceability but never logged
    /// in full to avoid leaking sensitive provider data.
    /// </summary>
    public string? ProviderReference { get; private set; }

    // ── Discovery metadata ───────────────────────────────────────────────────
    public DateTimeOffset DiscoveredAt { get; private set; }

    /// <summary>
    /// True for bills imported during the initial historical bootstrap.
    /// MUST suppress all notification logic. Never notify tenants about historical bills.
    /// </summary>
    public bool IsFromHistoricalBackfill { get; private set; }

    // ── Notification idempotency gate ────────────────────────────────────────
    /// <summary>
    /// Set once when the notification is dispatched. Never reset.
    /// Checked before creating any notification to prevent duplicates across
    /// retries, worker restarts, or concurrent Hangfire executions.
    /// </summary>
    public DateTimeOffset? NotificationSentAt { get; private set; }

    // ── Audit ────────────────────────────────────────────────────────────────
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // ── Concurrency token (PostgreSQL MVCC xmin) ─────────────────────────────
    public uint xmin { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static UtilityBill Create(
        Guid companyId,
        Guid utilityAccountId,
        UtilityType utilityType,
        string providerExternalId,
        DateOnly billDate,
        DateOnly? dueDate,
        decimal amount,
        string currency,
        bool isPaid,
        UtilityBillPaymentStatus paymentStatus,
        string? providerReference,
        bool isFromHistoricalBackfill,
        DateTimeOffset discoveredAt)
    {
        if (string.IsNullOrWhiteSpace(providerExternalId))
            throw new ArgumentException("Provider external ID must not be blank.", nameof(providerExternalId));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Bill amount must be positive.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new ArgumentException("Currency must be a 3-character ISO code.", nameof(currency));

        return new UtilityBill
        {
            Id                        = Guid.CreateVersion7(),
            CompanyId                 = companyId,
            UtilityAccountId          = utilityAccountId,
            UtilityType               = utilityType,
            ProviderExternalId        = providerExternalId.Trim(),
            BillDate                  = billDate,
            DueDate                   = dueDate,
            Amount                    = amount,
            Currency                  = currency.Trim().ToUpperInvariant(),
            IsPaid                    = isPaid,
            PaymentStatus             = paymentStatus,
            ProviderReference         = providerReference,
            IsFromHistoricalBackfill  = isFromHistoricalBackfill,
            DiscoveredAt              = discoveredAt,
            CreatedAt                 = discoveredAt,
            UpdatedAt                 = discoveredAt,
        };
    }

    // ── Notification idempotency ─────────────────────────────────────────────

    /// <summary>
    /// Marks this bill as having triggered a tenant notification.
    /// May only be called once; subsequent calls are no-ops (safe to call on retry).
    /// The caller is responsible for persisting the change.
    /// </summary>
    public void MarkNotificationSent(DateTimeOffset sentAt)
    {
        if (NotificationSentAt.HasValue)
            return; // Already sent; idempotent.

        NotificationSentAt = sentAt;
        UpdatedAt          = sentAt;
    }
}
