using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills;

/// <summary>
/// Repository interface for utility account persistence operations.
///
/// CQRS note: write-side operations only. Read-side projections go through
/// IApplicationDbContext directly in query handlers (no read repo layer).
///
/// All methods that cross company boundaries must receive an explicit companyId
/// parameter — never rely solely on RLS for authorization in query paths.
/// </summary>
public interface IUtilityAccountRepository
{
    Task AddAsync(UtilityAccount account, CancellationToken cancellationToken = default);

    Task<UtilityAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a previously unlinked exact subscription within an explicit company,
    /// lease, and utility-type boundary. This is the only write-side operation that
    /// deliberately bypasses the soft-delete query filter.
    /// </summary>
    Task<UtilityAccount?> GetDeletedExactMatchAsync(
        Guid companyId,
        Guid leaseContractId,
        UtilityType type,
        string accountNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if an active (non-deleted) utility account already exists for the
    /// given lease contract and utility type. Used to enforce the UNIQUE constraint
    /// at the application layer before attempting the DB insert.
    /// </summary>
    Task<bool> ExistsByLeaseAndTypeAsync(
        Guid leaseContractId,
        UtilityType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims an account for synchronisation using FOR UPDATE SKIP LOCKED.
    /// Returns the account if the claim succeeded, null if another worker already holds it
    /// or the account is stale-claimed beyond the threshold.
    ///
    /// The caller MUST call account.BeginClaim(jobRunId, claimedAt) on the returned
    /// entity to update in-memory state, then SaveChanges to persist the claim.
    /// </summary>
    Task<UtilityAccount?> TryClaimForSyncAsync(
        Guid id,
        DateTimeOffset staleClaimThreshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns account IDs eligible for synchronisation (keyset-cursor, NextCheckAt-gated).
    /// Only accounts whose historical_bootstrap_completed = true are returned by this method.
    /// The idx_utility_accounts_scheduler index makes this query efficient.
    ///
    /// Batch processing: pass afterId to continue from the last processed ID.
    /// </summary>
    Task<List<Guid>> GetDueAccountIdsAsync(
        UtilityType type,
        DateTimeOffset asOf,
        int batchSize,
        Guid? afterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns account IDs pending bootstrap (historical_bootstrap_completed = false).
    /// Keyset-cursor for bounded batch processing.
    /// </summary>
    Task<List<Guid>> GetPendingBootstrapIdsAsync(
        UtilityType type,
        int batchSize,
        Guid? afterId,
        CancellationToken cancellationToken = default);
}
