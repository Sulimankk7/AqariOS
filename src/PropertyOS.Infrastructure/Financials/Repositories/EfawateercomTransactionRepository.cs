using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Domain.Financials;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Financials.Repositories;

public class EfawateercomTransactionRepository : IEfawateercomTransactionRepository
{
    private readonly PropertyOsDbContext _dbContext;

    public EfawateercomTransactionRepository(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ── Write-side (aggregate loading) ──────────────────────────────────────

    public Task<EfawateercomTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.EfawateercomTransactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<EfawateercomTransaction?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EfawateercomTransactions
            .FromSqlRaw(
                "SELECT *, xmin FROM efawateercom_transactions WHERE id = {0} AND deleted_at IS NULL FOR UPDATE",
                id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<EfawateercomTransaction?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EfawateercomTransactions
            .FirstOrDefaultAsync(t => t.ExternalTransactionId == externalId, cancellationToken);
    }

    public async Task<EfawateercomTransaction?> GetByExternalIdForUpdateAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        // PostgreSQL SELECT ... FOR UPDATE acquires an exclusive row lock within the current transaction.
        // Concurrent handlers calling this for the same external ID will queue behind each other,
        // ensuring only one proceeds with a non-terminal status check at a time.
        return await _dbContext.EfawateercomTransactions
            .FromSqlRaw(
                "SELECT *, xmin FROM efawateercom_transactions WHERE external_transaction_id = {0} AND deleted_at IS NULL FOR UPDATE",
                externalId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(EfawateercomTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.EfawateercomTransactions.AddAsync(transaction, cancellationToken);
    }

    public Task<List<Guid>> GetStaleNonTerminalTransactionIdsAsync(DateTimeOffset olderThan, int batchSize, Guid? afterId, CancellationToken cancellationToken = default)
    {
        // Non-terminal (Pending/Sent) transactions whose gateway request is older than
        // the cutoff. Soft-deleted rows are excluded. Keyset sweep cursor: Id > afterId,
        // ordered by Id ascending — the caller advances the cursor per batch.
        var query = _dbContext.EfawateercomTransactions
            .AsNoTracking()
            .Where(t => (t.TransactionStatus == PropertyOS.Domain.Financials.Enums.EfawateercomStatus.Pending
                         || t.TransactionStatus == PropertyOS.Domain.Financials.Enums.EfawateercomStatus.Sent)
                        && t.RequestTime < olderThan
                        && t.DeletedAt == null);

        if (afterId.HasValue)
        {
            var cursor = afterId.Value;
            query = query.Where(t => t.Id.CompareTo(cursor) > 0);
        }

        return query
            .OrderBy(t => t.Id)
            .Take(batchSize)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    // ── Read-side (projections) ──────────────────────────────────────────────

    public Task<EfawateercomTransactionDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.EfawateercomTransactions
            .AsNoTracking()
            .Where(t => t.Id == id && t.DeletedAt == null)
            .ProjectToType<EfawateercomTransactionDetailDto>()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<EfawateercomTransactionDto>> GetTransactionsAsync(
        EfawateercomTransactionFilterOptions filter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EfawateercomTransactions
            .AsNoTracking()
            .Where(t => t.DeletedAt == null);

        // ── Filters ─────────────────────────────────────────────────────────
        if (filter.Status.HasValue)
            query = query.Where(t => t.TransactionStatus == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter.PaymentReference))
            query = query.Where(t => t.PaymentReference == filter.PaymentReference);

        if (filter.RentPaymentId.HasValue)
            query = query.Where(t => t.RentPaymentId == filter.RentPaymentId.Value);

        // ── Keyset pagination cursor: (RequestTime DESC, Id ASC) ────────────
        if (filter.LastSeenId.HasValue && filter.LastSeenRequestTime.HasValue)
        {
            var cursorTime = filter.LastSeenRequestTime.Value;
            var cursorId   = filter.LastSeenId.Value;

            query = query.Where(t =>
                t.RequestTime < cursorTime ||
                (t.RequestTime == cursorTime && t.Id.CompareTo(cursorId) > 0));
        }

        return query
            .OrderByDescending(t => t.RequestTime)
            .ThenBy(t => t.Id)
            .Take(filter.PageSize)
            .ProjectToType<EfawateercomTransactionDto>()
            .ToListAsync(cancellationToken);
    }
}
