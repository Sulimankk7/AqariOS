using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Domain.UtilityBills;

/// <summary>
/// Represents a tenant's linked utility account (electricity or water) for a lease contract.
///
/// Design rules:
///   - Private setters / private EF parameterless constructor (AqariOS convention).
///   - All state transitions go through explicit methods; no direct property mutation from outside.
///   - last_known_bill_date MUST NEVER be modified by failure paths. Only RecordSyncSuccess
///     and MarkBootstrapCompleted may touch it.
///   - Concurrency token: xmin (PostgreSQL MVCC system column).
/// </summary>
public sealed class UtilityAccount
{
    // ── EF Core parameterless constructor ───────────────────────────────────
    private UtilityAccount() { }

    // ── Identity ────────────────────────────────────────────────────────────
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid LeaseContractId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ApartmentId { get; private set; }

    // ── Account details ──────────────────────────────────────────────────────
    public UtilityType UtilityType { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public string? MeterNumber { get; private set; }
    public bool IsActive { get; private set; }
    public decimal? TotalOutstandingBalance { get; private set; }

    // ── Sync state ───────────────────────────────────────────────────────────
    /// <summary>
    /// The most recently confirmed bill date from the provider.
    /// INVARIANT: Never written by RecordSyncFailure. Only updated on confirmed success.
    /// </summary>
    public DateOnly? LastKnownBillDate { get; private set; }
    public DateTimeOffset? LastSuccessfulSyncAt { get; private set; }
    public DateTimeOffset? LastAttemptedSyncAt { get; private set; }

    /// <summary>
    /// The scheduler reads only this column to find due accounts.
    /// All scheduling logic reduces to: SELECT WHERE next_check_at &lt;= now().
    /// </summary>
    public DateTimeOffset NextCheckAt { get; private set; }

    public UtilitySyncStatus SyncStatus { get; private set; }
    public string? LastSyncErrorDetail { get; private set; }
    public short ConsecutiveFailureCount { get; private set; }

    // ── Water-only interval statistics ───────────────────────────────────────
    /// <summary>NULL for electricity accounts.</summary>
    public short? AverageBillingIntervalDays { get; private set; }
    public short BillingIntervalSampleCount { get; private set; }
    /// <summary>NULL for electricity accounts and when fewer than 2 water bills exist.</summary>
    public DateOnly? EstimatedNextBillDate { get; private set; }

    // ── Bootstrap flag ───────────────────────────────────────────────────────
    /// <summary>
    /// Set to true once the initial historical import completes.
    /// The scheduler index filters WHERE historical_bootstrap_completed = true,
    /// so newly created accounts are invisible to the scheduler until bootstrapped.
    /// </summary>
    public bool HistoricalBootstrapCompleted { get; private set; }

    // ── Concurrency claim ────────────────────────────────────────────────────
    public string? ClaimedByJobRunId { get; private set; }
    public DateTimeOffset? ClaimedAt { get; private set; }

    // ── Soft delete ──────────────────────────────────────────────────────────
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    // ── Audit ────────────────────────────────────────────────────────────────
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    // ── Concurrency token (PostgreSQL MVCC xmin — never assigned manually) ───
    public uint xmin { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new utility account in the NeverSynced / awaiting-bootstrap state.
    /// next_check_at is set to now() so the bootstrap job finds it immediately.
    /// historical_bootstrap_completed = false keeps it out of the scheduler index.
    /// </summary>
    public static UtilityAccount Create(
        Guid companyId,
        Guid leaseContractId,
        Guid tenantId,
        Guid apartmentId,
        UtilityType utilityType,
        string accountNumber,
        string? meterNumber,
        DateTimeOffset createdAt,
        Guid? createdBy)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("Account number must not be blank.", nameof(accountNumber));

        return new UtilityAccount
        {
            Id                            = Guid.CreateVersion7(),
            CompanyId                     = companyId,
            LeaseContractId               = leaseContractId,
            TenantId                      = tenantId,
            ApartmentId                   = apartmentId,
            UtilityType                   = utilityType,
            AccountNumber                 = accountNumber.Trim(),
            MeterNumber                   = meterNumber?.Trim(),
            IsActive                      = true,
            SyncStatus                    = UtilitySyncStatus.NeverSynced,
            ConsecutiveFailureCount       = 0,
            BillingIntervalSampleCount    = 0,
            HistoricalBootstrapCompleted  = false,
            // next_check_at = now() so the bootstrap job picks this up immediately
            NextCheckAt                   = createdAt,
            CreatedAt                     = createdAt,
            UpdatedAt                     = createdAt,
            CreatedBy                     = createdBy,
        };
    }

    // ── Concurrency claim ────────────────────────────────────────────────────

    /// <summary>
    /// Records that this worker has claimed this account for synchronisation.
    /// The actual row-level exclusive claim is enforced by the repository via
    /// FOR UPDATE SKIP LOCKED. This method updates domain state to match.
    /// </summary>
    public void BeginClaim(string jobRunId, DateTimeOffset claimedAt)
    {
        ClaimedByJobRunId = jobRunId;
        ClaimedAt         = claimedAt;
        SyncStatus        = UtilitySyncStatus.Syncing;
        UpdatedAt         = claimedAt;
    }

    /// <summary>
    /// Releases the concurrency claim after a sync attempt (success or failure).
    /// Always called at the end of SyncUtilityAccountCommandHandler.
    /// </summary>
    public void ReleaseClaim(DateTimeOffset updatedAt)
    {
        ClaimedByJobRunId = null;
        ClaimedAt         = null;
        UpdatedAt         = updatedAt;
    }

    // ── Sync outcomes ────────────────────────────────────────────────────────

    /// <summary>
    /// Records a successful provider sync. This is the ONLY method that may
    /// update LastKnownBillDate. Provider failure paths must NEVER call this.
    /// </summary>
    public void RecordSyncSuccess(
        DateOnly? lastKnownBillDate,
        DateTimeOffset syncedAt,
        DateTimeOffset nextCheckAt,
        decimal? totalOutstandingBalance = null)
    {
        if (totalOutstandingBalance < 0)
            throw new ArgumentOutOfRangeException(nameof(totalOutstandingBalance));

        LastKnownBillDate       = lastKnownBillDate;
        TotalOutstandingBalance = totalOutstandingBalance;
        LastSuccessfulSyncAt    = syncedAt;
        LastAttemptedSyncAt     = syncedAt;
        NextCheckAt             = nextCheckAt;
        SyncStatus              = UtilitySyncStatus.Synced;
        LastSyncErrorDetail     = null;
        ConsecutiveFailureCount = 0;
        UpdatedAt               = syncedAt;
    }

    /// <summary>
    /// Records a provider failure. MUST NOT modify LastKnownBillDate.
    /// Provider failure ≠ "no bill". Existing data is preserved.
    /// </summary>
    public void RecordSyncFailure(
        UtilitySyncStatus status,
        string? errorDetail,
        DateTimeOffset attemptedAt,
        DateTimeOffset nextCheckAt)
    {
        // Invariant: LastKnownBillDate is deliberately NOT modified here.
        LastAttemptedSyncAt     = attemptedAt;
        NextCheckAt             = nextCheckAt;
        SyncStatus              = status;
        LastSyncErrorDetail     = errorDetail;
        ConsecutiveFailureCount = (short)(ConsecutiveFailureCount + 1);
        UpdatedAt               = attemptedAt;
    }

    /// <summary>
    /// Records a bounded sync attempt when the provider integration is unavailable by
    /// configuration. This is observable as ProviderError but does not count toward
    /// automatic suspension because no provider request was made.
    /// </summary>
    public void RecordProviderNotConfigured(
        string? errorDetail,
        DateTimeOffset attemptedAt,
        DateTimeOffset nextCheckAt)
    {
        LastAttemptedSyncAt = attemptedAt;
        NextCheckAt = nextCheckAt;
        SyncStatus = UtilitySyncStatus.ProviderError;
        LastSyncErrorDetail = errorDetail;
        UpdatedAt = attemptedAt;
    }

    /// <summary>
    /// Suspends the account after exceeding the maximum consecutive failure count.
    /// No further automatic scheduling will occur until IsActive is restored.
    /// </summary>
    public void Suspend(DateTimeOffset suspendedAt)
    {
        IsActive   = false;
        SyncStatus = UtilitySyncStatus.Suspended;
        UpdatedAt  = suspendedAt;
    }

    /// <summary>
    /// Re-enables the same previously unlinked external subscription. Historical
    /// bills remain attached to this account, preserving provider-level idempotency.
    /// </summary>
    public void Reactivate(
        string accountNumber,
        string? meterNumber,
        DateTimeOffset reactivatedAt,
        Guid reactivatedBy)
    {
        var normalizedAccountNumber = accountNumber.Trim();
        if (!string.Equals(AccountNumber, normalizedAccountNumber, StringComparison.Ordinal))
            throw new InvalidOperationException("Only an exact utility account match can be reactivated.");

        AccountNumber = normalizedAccountNumber;
        MeterNumber = meterNumber?.Trim();
        IsActive = true;
        DeletedAt = null;
        DeletedBy = null;
        ClaimedAt = null;
        ClaimedByJobRunId = null;
        SyncStatus = UtilitySyncStatus.NeverSynced;
        LastSyncErrorDetail = null;
        ConsecutiveFailureCount = 0;
        NextCheckAt = reactivatedAt;
        UpdatedAt = reactivatedAt;
        UpdatedBy = reactivatedBy;
    }

    /// <summary>
    /// Updates ancillary link details without changing the external subscription
    /// identity, then places the account back into the asynchronous verification flow.
    /// </summary>
    public void RefreshLinkDetails(
        string accountNumber,
        string? meterNumber,
        DateTimeOffset updatedAt,
        Guid updatedBy)
    {
        var normalizedAccountNumber = accountNumber.Trim();
        if (!string.Equals(AccountNumber, normalizedAccountNumber, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "An existing utility account number cannot be mutated in place.");

        MeterNumber = meterNumber?.Trim();
        IsActive = true;
        SyncStatus = UtilitySyncStatus.NeverSynced;
        LastSyncErrorDetail = null;
        ConsecutiveFailureCount = 0;
        NextCheckAt = updatedAt;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    /// <summary>Re-enables a suspended account before a manual background retry.</summary>
    public void PrepareManualRetry(DateTimeOffset requestedAt, Guid requestedBy)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("An unlinked utility account cannot be retried.");

        if (SyncStatus == UtilitySyncStatus.Suspended)
            IsActive = true;

        NextCheckAt = requestedAt;
        UpdatedAt = requestedAt;
        UpdatedBy = requestedBy;
    }

    // ── Water-specific interval updates ─────────────────────────────────────

    /// <summary>
    /// Updates water billing interval statistics incrementally after a newly
    /// discovered bill. Never reloads historical data.
    ///
    /// Formula: newAvg = (oldAvg * oldCount + newGapDays) / (oldCount + 1)
    /// </summary>
    public void UpdateWaterIntervalStatistics(
        short newAverageBillingIntervalDays,
        short newSampleCount,
        DateOnly estimatedNextBillDate,
        DateTimeOffset nextCheckAt,
        DateTimeOffset updatedAt)
    {
        AverageBillingIntervalDays = newAverageBillingIntervalDays;
        BillingIntervalSampleCount = newSampleCount;
        EstimatedNextBillDate      = estimatedNextBillDate;
        NextCheckAt                = nextCheckAt;
        UpdatedAt                  = updatedAt;
    }

    // ── Bootstrap completion ─────────────────────────────────────────────────

    /// <summary>
    /// Called once after the initial historical import completes.
    /// Sets historical_bootstrap_completed = true which makes this account
    /// visible to the scheduler index (idx_utility_accounts_scheduler).
    /// </summary>
    public void MarkBootstrapCompleted(
        DateOnly? lastKnownBillDate,
        short? averageBillingIntervalDays,
        short sampleCount,
        DateOnly? estimatedNextBillDate,
        DateTimeOffset syncedAt,
        DateTimeOffset nextCheckAt,
        decimal? totalOutstandingBalance = null)
    {
        if (totalOutstandingBalance < 0)
            throw new ArgumentOutOfRangeException(nameof(totalOutstandingBalance));

        HistoricalBootstrapCompleted = true;
        LastKnownBillDate            = lastKnownBillDate;
        TotalOutstandingBalance      = totalOutstandingBalance;
        AverageBillingIntervalDays   = averageBillingIntervalDays;
        BillingIntervalSampleCount   = sampleCount;
        EstimatedNextBillDate        = estimatedNextBillDate;
        LastSuccessfulSyncAt         = syncedAt;
        LastAttemptedSyncAt          = syncedAt;
        NextCheckAt                  = nextCheckAt;
        SyncStatus                   = UtilitySyncStatus.Synced;
        LastSyncErrorDetail          = null;
        ConsecutiveFailureCount      = 0;
        UpdatedAt                    = syncedAt;
    }

    // ── Soft delete ──────────────────────────────────────────────────────────

    public void SoftDelete(DateTimeOffset deletedAt, Guid deletedBy)
    {
        IsActive         = false;
        ClaimedAt        = null;
        ClaimedByJobRunId = null;
        DeletedAt        = deletedAt;
        DeletedBy        = deletedBy;
        UpdatedAt        = deletedAt;
        UpdatedBy        = deletedBy;
    }
}
