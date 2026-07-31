using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Commands.ExpireLeaseContract;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Infrastructure.Identity;

namespace PropertyOS.Infrastructure.Leasing.Jobs;

/// <summary>
/// Background job that expires active lease contracts whose end date has been reached
/// (relative to the Jordan business calendar: Asia/Amman).
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
///   After MaxPoisonContractsPerCompany contract failures for a single company,
///   the remaining batches for that company are abandoned. The poison set stays
///   job-side (in memory) only — it is never sent to SQL.
///   The threshold is a named constant; operational impact is documented below.
///
/// BATCH SIZE / CURSOR:
///   DefaultBatchSize controls how many contract IDs are materialized per
///   eligibility query. Batches advance via a keyset cursor: each eligibility
///   query returns IDs strictly greater than the last ID of the previous batch
///   (ordered by Id). Each batch of N produces exactly:
///     - 1 eligibility query (reads IDs)
///     - N point-read + update transactions (one per contract command)
///   The final batch of any company produces 1 empty eligibility query → break.
///   Total eligibility queries per company with N eligible contracts, no poison:
///     ceil(N / batchSize) + 1.
///   For N = 0: 1 eligibility query.
///
/// FAILURE ISOLATION:
///   Platform enumeration failure → job-level exception (propagates to Hangfire).
///   Company-level failure         → log error, skip company, continue next.
///   Per-contract failure          → log warning, add to poison list, continue.
///   Poison threshold exceeded     → log error, abandon company, continue next.
/// </summary>
public class ExpireLeaseContractsJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBusinessClock _businessClock;
    private readonly ILogger<ExpireLeaseContractsJob> _logger;

    public const int DefaultBatchSize = 100;

    /// <summary>
    /// Maximum number of individual contract failures allowed per company per sweep.
    /// Once exceeded, the remaining batches for that company are abandoned — a sweep
    /// drowning in failures almost certainly has a systemic cause. The poison set is
    /// job-side memory only.
    ///
    /// OPERATIONAL TRADEOFF:
    ///   A value of 50 means: if 50 contracts fail due to transient infrastructure
    ///   errors in a single sweep, we stop attempting further contracts for that
    ///   company. The next scheduled sweep starts fresh (empty poison set) while
    ///   still processing most contracts.
    ///   Set to match DefaultBatchSize / 2 as a conservative default.
    /// </summary>
    public const int MaxPoisonContractsPerCompany = 50;

    public ExpireLeaseContractsJob(
        IServiceProvider serviceProvider,
        IBusinessClock businessClock,
        ILogger<ExpireLeaseContractsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _businessClock = businessClock;
        _logger = logger;
    }

    /// <summary>
    /// Executes the full multi-tenant lease expiration sweep.
    /// </summary>
    /// <param name="asOf">
    /// Optional override for the reference UTC instant. When null, uses the
    /// current wall-clock UTC time. The same instant is used to derive the
    /// Jordan business date (for eligibility) and the expiration timestamp
    /// (for audit). No multiple wall-clock reads occur per sweep.
    /// </param>
    /// <param name="batchSize">Number of contract IDs to retrieve per eligibility query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total number of successfully expired contracts across all companies.</returns>
    public async Task<int> ExecuteSweepAsync(
        DateTimeOffset? asOf = null,
        int batchSize = DefaultBatchSize,
        CancellationToken cancellationToken = default)
    {
        var effectiveAsOf = asOf ?? _businessClock.UtcNow;
        var jordanBusinessDate = _businessClock.GetJordanBusinessDate(effectiveAsOf);

        _logger.LogInformation(
            "Starting multi-tenant lease expiration sweep for Jordan business date {JordanDate} (UTC instant: {UtcNow}).",
            jordanBusinessDate, effectiveAsOf);

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

            companyIds = await dbContext.ExecuteInTransactionAsync(async (ct) =>
            {
                var companyRepo = platformScope.ServiceProvider
                    .GetRequiredService<ICompanyRepository>();

                return await companyRepo.GetActiveCompanyIdsAsync(ct);
            }, cancellationToken);
            // ↑ SET LOCAL is reset — no tenant state leaks through connection pooling
        }

        _logger.LogInformation(
            "Found {CompanyCount} active companies for lease expiration processing.",
            companyIds.Count);

        int totalSuccessfullyExpired = 0;

        foreach (var companyId in companyIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var companyExpiredCount = await ProcessCompanyLeaseExpirationAsync(
                    companyId, jordanBusinessDate, effectiveAsOf, batchSize, cancellationToken);
                totalSuccessfullyExpired += companyExpiredCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Company-level failure during lease expiration sweep for company {CompanyId}. " +
                    "Skipping this company; remaining companies will continue.",
                    companyId);
            }
        }

        _logger.LogInformation(
            "Completed multi-tenant lease expiration sweep. Total expired contracts: {TotalExpired}.",
            totalSuccessfullyExpired);

        return totalSuccessfullyExpired;
    }

    private async Task<int> ProcessCompanyLeaseExpirationAsync(
        Guid companyId,
        DateOnly jordanBusinessDate,
        DateTimeOffset effectiveAsOf,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int companyExpiredCount = 0;
        Guid? afterContractId = null;
        var poisonContractIds = new HashSet<Guid>();

        while (!cancellationToken.IsCancellationRequested)
        {
            // -----------------------------------------------------------------
            // Step 2: Eligibility query — enumerate expiring contract IDs.
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
            List<Guid> batchContractIds;
            using (var eligibilityScope = _serviceProvider.CreateScope())
            {
                var tenantSetter = eligibilityScope.ServiceProvider
                    .GetRequiredService<ISystemTenantContextSetter>();
                tenantSetter.SetCompanyScope(companyId);
                // ↑ Must happen BEFORE BeginTransactionAsync

                var dbContext = eligibilityScope.ServiceProvider
                    .GetRequiredService<IApplicationDbContext>();

                batchContractIds = await dbContext.ExecuteInTransactionAsync(async (ct) =>
                {
                    var contractRepo = eligibilityScope.ServiceProvider
                        .GetRequiredService<ILeaseContractRepository>();

                    return await contractRepo.GetActiveContractIdsExpiringOnOrBeforeAsync(
                        jordanBusinessDate, batchSize, afterContractId, ct);
                }, cancellationToken);
            }

            if (batchContractIds.Count == 0)
                break;

            // Keyset cursor: results are ordered by Id, so the last returned ID is
            // the cursor for the next batch.
            afterContractId = batchContractIds[^1];

            _logger.LogInformation(
                "Company {CompanyId}: Found batch of {BatchCount} expiring active lease contracts.",
                companyId, batchContractIds.Count);

            // -----------------------------------------------------------------
            // Step 3: Expire each contract individually.
            //
            // Each contract gets its own DI scope so that:
            //   - DbContext change tracker is fresh per command.
            //   - Tenant context is independently established.
            //   - TransactionBehavior manages the transaction for each command.
            //
            // Poison threshold: if failures accumulate beyond the threshold,
            // we abort this company's processing — a systemic cause is far more
            // likely than dozens of independent per-contract defects.
            // -----------------------------------------------------------------
            foreach (var contractId in batchContractIds)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (poisonContractIds.Count >= MaxPoisonContractsPerCompany)
                {
                    _logger.LogError(
                        "Company {CompanyId}: Poison threshold of {Threshold} failed contracts reached. " +
                        "Abandoning remaining batches for this company. Failed contract IDs will be retried on the next sweep.",
                        companyId, MaxPoisonContractsPerCompany);
                    return companyExpiredCount;
                }

                // Client-side poison skip: the cursor normally advances past poison
                // IDs, but never re-attempt one within the same sweep.
                if (poisonContractIds.Contains(contractId))
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
                        new ExpireLeaseContractCommand(contractId, effectiveAsOf),
                        cancellationToken);
                    companyExpiredCount++;
                }
                catch (Exception ex)
                {
                    poisonContractIds.Add(contractId);
                    _logger.LogWarning(
                        ex,
                        "Failed to expire lease contract {ContractId} for company {CompanyId}. " +
                        "Contract added to poison exclusion list for this sweep " +
                        "(poison count: {PoisonCount}/{PoisonThreshold}).",
                        contractId, companyId,
                        poisonContractIds.Count, MaxPoisonContractsPerCompany);
                }
            }
        }

        return companyExpiredCount;
    }
}
