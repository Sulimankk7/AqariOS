using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials;

public interface IEfawateercomTransactionRepository
{
    Task<Domain.Financials.EfawateercomTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the transaction by internal ID using SELECT ... FOR UPDATE.
    /// Use for commands that know the internal ID (e.g. MarkSent, Expire, Cancel)
    /// and need to serialize concurrent state transitions at the DB row-lock level.
    /// Must only be called within an open database transaction.
    /// </summary>
    Task<Domain.Financials.EfawateercomTransaction?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Domain.Financials.EfawateercomTransaction?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the transaction using SELECT ... FOR UPDATE to acquire an exclusive row-level lock.
    /// Must only be called within an open database transaction (i.e., inside the TransactionBehavior pipeline).
    /// Use this instead of GetByExternalIdAsync when the caller must serialize concurrent callbacks.
    /// </summary>
    Task<Domain.Financials.EfawateercomTransaction?> GetByExternalIdForUpdateAsync(string externalId, CancellationToken cancellationToken = default);

    Task AddAsync(Domain.Financials.EfawateercomTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates the IDs of non-deleted transactions still in a non-terminal state
    /// (Pending or Sent) whose RequestTime is older than the given cutoff. Keyset sweep
    /// contract: ordered by Id ascending, limited to batchSize, returning only IDs
    /// strictly greater than <paramref name="afterId"/> (null = start of sweep). The
    /// caller advances the cursor with the last returned ID per batch; poison IDs are
    /// skipped client-side.
    /// </summary>
    Task<List<Guid>> GetStaleNonTerminalTransactionIdsAsync(DateTimeOffset olderThan, int batchSize, Guid? afterId, CancellationToken cancellationToken = default);

    // Read-side projections
    Task<EfawateercomTransactionDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<EfawateercomTransactionDto>> GetTransactionsAsync(EfawateercomTransactionFilterOptions filter, CancellationToken cancellationToken = default);
}

public record EfawateercomTransactionFilterOptions(
    EfawateercomStatus? Status = null,
    string? PaymentReference = null,
    Guid? RentPaymentId = null,
    Guid? LastSeenId = null,
    DateTimeOffset? LastSeenRequestTime = null,
    int PageSize = 50
);
