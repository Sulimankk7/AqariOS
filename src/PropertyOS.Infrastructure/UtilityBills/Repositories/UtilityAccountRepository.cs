using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.UtilityBills;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.UtilityBills.Repositories;

internal sealed class UtilityAccountRepository : IUtilityAccountRepository
{
    private readonly PropertyOsDbContext _context;

    public UtilityAccountRepository(PropertyOsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UtilityAccount account, CancellationToken cancellationToken = default)
    {
        await _context.UtilityAccounts.AddAsync(account, cancellationToken);
        // SaveChanges is handled by TransactionBehavior — never called here.
    }

    public async Task<UtilityAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UtilityAccounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<UtilityAccount?> GetDeletedExactMatchAsync(
        Guid companyId,
        Guid leaseContractId,
        UtilityType type,
        string accountNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedAccountNumber = accountNumber.Trim();
        return await _context.UtilityAccounts
            .IgnoreQueryFilters()
            .Where(a => a.CompanyId == companyId
                     && a.LeaseContractId == leaseContractId
                     && a.UtilityType == type
                     && a.AccountNumber == normalizedAccountNumber
                     && a.DeletedAt != null)
            .OrderByDescending(a => a.LastKnownBillDate)
            .ThenByDescending(a => a.DeletedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsByLeaseAndTypeAsync(
        Guid leaseContractId,
        UtilityType type,
        CancellationToken cancellationToken = default)
    {
        return await _context.UtilityAccounts
            .AnyAsync(a => a.LeaseContractId == leaseContractId && a.UtilityType == type,
                cancellationToken);
        // Note: HasQueryFilter(a => a.DeletedAt == null) already applied by EF Core.
    }

    /// <summary>
    /// Atomically claims an account for synchronisation using FOR UPDATE SKIP LOCKED.
    ///
    /// Two-step process:
    ///   1. Try to lock the row with FOR UPDATE SKIP LOCKED.
    ///      If another connection holds the lock, this returns immediately with null.
    ///   2. If the lock is obtained, update claim fields and return the entity.
    ///
    /// The caller (job handler) commits this short transaction before calling the provider.
    /// Provider HTTP calls must NEVER happen while holding a DB transaction.
    /// </summary>
    public async Task<UtilityAccount?> TryClaimForSyncAsync(
        Guid id,
        DateTimeOffset staleClaimThreshold,
        CancellationToken cancellationToken = default)
    {
        // Raw SQL for FOR UPDATE SKIP LOCKED — EF Core does not generate this natively.
        // Returns the account only if:
        //   - It exists and is active and not soft-deleted
        //   - It has no current claim OR the claim is stale (worker crashed)
        var sql = """
            SELECT id AS "Value" FROM utility_accounts
            WHERE id = {0}
              AND deleted_at IS NULL
              AND is_active = true
              AND (claimed_at IS NULL OR claimed_at < {1})
            FOR UPDATE SKIP LOCKED
            LIMIT 1
            """;


        var found = await _context.Database
            .SqlQueryRaw<Guid>(sql, id, staleClaimThreshold)
            .FirstOrDefaultAsync(cancellationToken);

        if (found == Guid.Empty)
            return null;

        // Load the entity into the change tracker now that we hold the lock.
        // Use the same DbContext instance so the entity is tracked for the subsequent update.
        return await _context.UtilityAccounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<List<Guid>> GetDueAccountIdsAsync(
        UtilityType type,
        DateTimeOffset asOf,
        int batchSize,
        Guid? afterId,
        CancellationToken cancellationToken = default)
    {
        // Uses idx_utility_accounts_scheduler partial index for efficiency.
        // HasQueryFilter (deleted_at IS NULL) is already applied by EF Core.
        var query = _context.UtilityAccounts
            .AsNoTracking()
            .Where(a => a.UtilityType == type
                     && a.NextCheckAt <= asOf
                     && a.IsActive
                     && a.HistoricalBootstrapCompleted);

        if (afterId.HasValue)
            query = query.Where(a => a.Id > afterId.Value);

        return await query
            .OrderBy(a => a.Id)
            .Take(batchSize)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetPendingBootstrapIdsAsync(
        UtilityType type,
        int batchSize,
        Guid? afterId,
        CancellationToken cancellationToken = default)
    {
        // Uses idx_utility_accounts_bootstrap_pending partial index.
        // HasQueryFilter (deleted_at IS NULL) already applied.
        var query = _context.UtilityAccounts
            .AsNoTracking()
            .Where(a => a.UtilityType == type
                     && a.IsActive
                     && !a.HistoricalBootstrapCompleted);

        if (afterId.HasValue)
            query = query.Where(a => a.Id > afterId.Value);

        return await query
            .OrderBy(a => a.Id)
            .Take(batchSize)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);
    }
}
