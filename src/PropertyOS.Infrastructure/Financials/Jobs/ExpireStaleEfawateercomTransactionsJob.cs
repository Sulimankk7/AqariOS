using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Commands.ExpireEfawateercomTransaction;
using PropertyOS.Infrastructure.Identity;

namespace PropertyOS.Infrastructure.Financials.Jobs;

/// <summary>
/// Background job that expires stale eFAWATEERcom transactions: rows still in a
/// non-terminal state (Pending or Sent) whose gateway request is older than the
/// configured staleness window are transitioned to Timeout via
/// ExpireEfawateercomTransactionCommand. The per-transaction handler locks the row
/// (FOR UPDATE) and is a no-op for already-terminal transactions, so the sweep is
/// idempotent and safe against a late gateway callback racing the expiry.
///
/// MULTI-TENANT ARCHITECTURE:
///   The job operates across all active companies. For each operation it creates a
///   fresh, isolated DI scope to guarantee that no tenant context leaks between
///   companies. The critical ordering requirement is:
///
///     1. Resolve ISystemTenantContextSetter from the scope.
///     2. Call SetCompanyScope / SetPlatformAdminScope.
///     3. Resolve IApplicationDbContext from the SAME scope.
///     4. Call BeginTransactionAsync — TenantSessionInterceptor fires here
///        and reads the already-set tenant context to issue:
///           SET LOCAL app.current_company_id = '...'
///           SET LOCAL app.is_platform_admin = 'true/false'
///     5. Execute the query/command within that transaction.
///     6. Commit and dispose the scope.
///
///   Any reordering of steps 2 and 4 breaks RLS.
///
/// POISON THRESHOLD:
///   After MaxPoisonTransactionsPerCompany transaction failures for a single company,
///   the remaining batches for that company are abandoned. The poison set stays
///   job-side (in memory) only — it is never sent to SQL.
///
/// BATCH SIZE / CURSOR:
///   DefaultBatchSize controls how many transaction IDs are materialized per
///   eligibility query. Batches advance via a keyset cursor: each eligibility
///   query returns IDs strictly greater than the last ID of the previous batch
///   (ordered by Id), so the loop strictly advances regardless of per-item
///   success or failure.
///
/// FAILURE ISOLATION:
///   Platform enumeration failure → job-level exception (propagates to Hangfire).
///   Company-level failure         → log error, skip company, continue next.
///   Per-transaction failure       → log warning, add to poison list, continue.
///   Poison threshold exceeded     → log error, abandon company, continue next.
/// </summary>
public class ExpireStaleEfawateercomTransactionsJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBusinessClock _businessClock;
    private readonly ILogger<ExpireStaleEfawateercomTransactionsJob> _logger;
    private readonly int _staleAfterMinutes;

    public const int DefaultBatchSize = 100;

    /// <summary>
    /// Default staleness window: a non-terminal transaction whose RequestTime is older
    /// than this many minutes is considered abandoned by the gateway and expired.
    /// The orchestrator may override this via configuration when wiring DI.
    /// </summary>
    public const int DefaultStaleAfterMinutes = 60;

    /// <summary>
    /// Maximum number of individual transaction failures allowed per company per sweep.
    /// Once exceeded, the remaining batches for that company are abandoned — a sweep
    /// drowning in failures almost certainly has a systemic cause. The poison set is
    /// job-side memory only; the next scheduled sweep starts fresh.
    /// </summary>
    public const int MaxPoisonTransactionsPerCompany = 50;

    public ExpireStaleEfawateercomTransactionsJob(
        IServiceProvider serviceProvider,
        IBusinessClock businessClock,
        ILogger<ExpireStaleEfawateercomTransactionsJob> logger,
        int staleAfterMinutes = DefaultStaleAfterMinutes)
    {
        if (staleAfterMinutes <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(staleAfterMinutes), staleAfterMinutes, "Staleness window must be positive.");

        _serviceProvider = serviceProvider;
        _businessClock = businessClock;
        _logger = logger;
        _staleAfterMinutes = staleAfterMinutes;
    }

    /// <summary>
    /// Executes the full multi-tenant stale-transaction expiry sweep.
    /// </summary>
    /// <param name="asOf">
    /// Optional override for the reference UTC instant. When null, uses the
    /// current wall-clock UTC time. The staleness cutoff is derived from this
    /// single instant (asOf - staleAfterMinutes); no multiple wall-clock reads
    /// occur per sweep.
    /// </param>
    /// <param name="batchSize">Number of transaction IDs to retrieve per eligibility query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total number of successfully expired transactions across all companies.</returns>
    public async Task<int> ExecuteSweepAsync(
        DateTimeOffset? asOf = null,
        int batchSize = DefaultBatchSize,
        CancellationToken cancellationToken = default)
    {
        var effectiveAsOf = asOf ?? _businessClock.UtcNow;
        var staleCutoff = effectiveAsOf.AddMinutes(-_staleAfterMinutes);

        _logger.LogInformation(
            "Starting multi-tenant stale eFAWATEERcom transaction expiry sweep. " +
            "Cutoff: {StaleCutoff} (UTC instant: {UtcNow}, window: {StaleAfterMinutes} minutes).",
            staleCutoff, effectiveAsOf, _staleAfterMinutes);

        // -----------------------------------------------------------------------
        // Step 1: Enumerate active companies using platform-admin tenant scope.
        //
        // TRANSACTION REQUIREMENT:
        //   TenantSessionInterceptor only fires on BeginTransaction. We MUST open
        //   a transaction before querying to establish the RLS session variable.
        //   Without this, app.is_platform_admin is never set and the companies
        //   table RLS policy denies the query, returning an empty list silently.
        //
        // PROPAGATION: Platform enumeration failure is NOT swallowed. It propagates
        //   as a job-level failure (Hangfire marks the invocation as failed).
        // -----------------------------------------------------------------------
        List<Guid> companyIds;
        using (var platformScope = _serviceProvider.CreateScope())
        {
            var tenantSetter = platformScope.ServiceProvider
                .GetRequiredService<ISystemTenantContextSetter>();
            tenantSetter.SetPlatformAdminScope();
            // ↑ Must happen BEFORE BeginTransactionAsync

            var dbContext = platformScope.ServiceProvider
                .GetRequiredService<IApplicationDbContext>();

            await using var tx = await dbContext.BeginTransactionAsync(cancellationToken);
            // ↑ TenantSessionInterceptor fires here:
            //   SET LOCAL app.is_platform_admin = 'true'

            var companyRepo = platformScope.ServiceProvider
                .GetRequiredService<ICompanyRepository>();

            companyIds = await companyRepo.GetActiveCompanyIdsAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);
            // ↑ SET LOCAL is reset — no tenant state leaks through connection pooling
        }

        _logger.LogInformation(
            "Found {CompanyCount} active companies for stale eFAWATEERcom transaction processing.",
            companyIds.Count);

        int totalSuccessfullyExpired = 0;

        foreach (var companyId in companyIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var companyExpiredCount = await ProcessCompanyStaleTransactionsAsync(
                    companyId, staleCutoff, batchSize, cancellationToken);
                totalSuccessfullyExpired += companyExpiredCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Company-level failure during stale eFAWATEERcom transaction sweep for company {CompanyId}. " +
                    "Skipping this company; remaining companies will continue.",
                    companyId);
            }
        }

        _logger.LogInformation(
            "Completed multi-tenant stale eFAWATEERcom transaction sweep. Total expired transactions: {TotalExpired}.",
            totalSuccessfullyExpired);

        return totalSuccessfullyExpired;
    }

    private async Task<int> ProcessCompanyStaleTransactionsAsync(
        Guid companyId,
        DateTimeOffset staleCutoff,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int companyExpiredCount = 0;
        Guid? afterTransactionId = null;
        var poisonTransactionIds = new HashSet<Guid>();

        while (!cancellationToken.IsCancellationRequested)
        {
            // -----------------------------------------------------------------
            // Step 2: Eligibility query — enumerate stale non-terminal transaction IDs.
            //
            // TRANSACTION REQUIREMENT:
            //   Same as platform enumeration above. We MUST open a transaction
            //   before querying so TenantSessionInterceptor fires and sets:
            //     SET LOCAL app.current_company_id = '<companyId>'
            //   Without this, RLS returns 0 rows silently.
            //
            // A fresh scope is created per batch to prevent DbContext change
            // tracker accumulation and to ensure clean tenant context isolation.
            // -----------------------------------------------------------------
            List<Guid> batchTransactionIds;
            using (var eligibilityScope = _serviceProvider.CreateScope())
            {
                var tenantSetter = eligibilityScope.ServiceProvider
                    .GetRequiredService<ISystemTenantContextSetter>();
                tenantSetter.SetCompanyScope(companyId);
                // ↑ Must happen BEFORE BeginTransactionAsync

                var dbContext = eligibilityScope.ServiceProvider
                    .GetRequiredService<IApplicationDbContext>();

                await using var tx = await dbContext.BeginTransactionAsync(cancellationToken);
                // ↑ TenantSessionInterceptor fires:
                //   SET LOCAL app.current_company_id = '<companyId>'
                //   SET LOCAL app.is_platform_admin = 'false'

                var transactionRepo = eligibilityScope.ServiceProvider
                    .GetRequiredService<IEfawateercomTransactionRepository>();

                batchTransactionIds = await transactionRepo.GetStaleNonTerminalTransactionIdsAsync(
                    staleCutoff, batchSize, afterTransactionId, cancellationToken);

                await tx.CommitAsync(cancellationToken);
            }

            if (batchTransactionIds.Count == 0)
                break;

            // Keyset cursor: results are ordered by Id, so the last returned ID is
            // the cursor for the next batch.
            afterTransactionId = batchTransactionIds[^1];

            _logger.LogInformation(
                "Company {CompanyId}: Found batch of {BatchCount} stale non-terminal eFAWATEERcom transactions.",
                companyId, batchTransactionIds.Count);

            // -----------------------------------------------------------------
            // Step 3: Expire each transaction individually.
            //
            // Each transaction gets its own DI scope so that:
            //   - DbContext change tracker is fresh per command.
            //   - Tenant context is independently established.
            //   - TransactionBehavior manages the transaction for each command.
            //
            // Poison threshold: if failures accumulate beyond the threshold,
            // we abort this company's processing — a systemic cause is far more
            // likely than dozens of independent per-transaction defects.
            // -----------------------------------------------------------------
            foreach (var transactionId in batchTransactionIds)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (poisonTransactionIds.Count >= MaxPoisonTransactionsPerCompany)
                {
                    _logger.LogError(
                        "Company {CompanyId}: Poison threshold of {Threshold} failed transactions reached. " +
                        "Abandoning remaining batches for this company. Failed transaction IDs will be retried on the next sweep.",
                        companyId, MaxPoisonTransactionsPerCompany);
                    return companyExpiredCount;
                }

                // Client-side poison skip: the cursor normally advances past poison
                // IDs, but never re-attempt one within the same sweep.
                if (poisonTransactionIds.Contains(transactionId))
                    continue;

                using var itemScope = _serviceProvider.CreateScope();
                var tenantSetter = itemScope.ServiceProvider
                    .GetRequiredService<ISystemTenantContextSetter>();
                tenantSetter.SetCompanyScope(companyId);
                // ↑ Must happen BEFORE sender.Send which triggers TransactionBehavior

                var sender = itemScope.ServiceProvider.GetRequiredService<ISender>();

                try
                {
                    // TransactionBehavior → BeginTransactionAsync → TenantSessionInterceptor
                    // → handler → commit — all within itemScope.
                    await sender.Send(
                        new ExpireEfawateercomTransactionCommand(transactionId),
                        cancellationToken);
                    companyExpiredCount++;
                }
                catch (Exception ex)
                {
                    poisonTransactionIds.Add(transactionId);
                    _logger.LogWarning(
                        ex,
                        "Failed to expire eFAWATEERcom transaction {TransactionId} for company {CompanyId}. " +
                        "Transaction added to poison exclusion list for this sweep " +
                        "(poison count: {PoisonCount}/{PoisonThreshold}).",
                        transactionId, companyId,
                        poisonTransactionIds.Count, MaxPoisonTransactionsPerCompany);
                }
            }
        }

        return companyExpiredCount;
    }
}
