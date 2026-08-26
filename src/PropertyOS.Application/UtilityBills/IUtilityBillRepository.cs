using PropertyOS.Domain.UtilityBills;

namespace PropertyOS.Application.UtilityBills;

/// <summary>
/// Repository interface for utility bill persistence operations.
///
/// CQRS note: write-side operations only (insert + notification state update).
/// Read-side projections go through IApplicationDbContext directly in query handlers.
/// </summary>
public interface IUtilityBillRepository
{
    /// <summary>
    /// Inserts a utility bill if no bill with the same (utility_account_id, provider_external_id)
    /// already exists. Uses ON CONFLICT (utility_account_id, provider_external_id) DO NOTHING.
    ///
    /// Returns true if the bill was inserted (new bill discovered).
    /// Returns false if the bill already existed (idempotent re-processing / Hangfire retry).
    ///
    /// This is the database-level duplicate protection mandated by the spec.
    /// Application-level checks alone are insufficient.
    /// </summary>
    Task<bool> InsertIfNewAsync(UtilityBill bill, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all unpaid, unnotified bills for a utility account.
    /// Used by SyncUtilityAccountCommandHandler to determine which bills need notifications.
    /// Only returns bills where notification_sent_at IS NULL AND is_paid = false
    /// AND is_from_historical_backfill = false.
    /// </summary>
    Task<List<UtilityBill>> GetUnnotifiedUnpaidBillsAsync(
        Guid utilityAccountId,
        CancellationToken cancellationToken = default);
}
