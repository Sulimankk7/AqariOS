using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies;
using PropertyOS.Application.Financials.Commands.GenerateScheduledInstallments;
using PropertyOS.Application.Leasing;
using PropertyOS.Infrastructure.Identity;

namespace PropertyOS.Infrastructure.Financials.Jobs;

/// <summary>
/// Background job that (re)generates scheduled rent installments for every active
/// lease contract across all active companies. The per-contract command handler
/// dedupes billing periods (HasScheduledInstallmentAsync), so the sweep is
/// idempotent by design: re-running it never duplicates installments.
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
///
/// BATCH SIZE / CURSOR:
///   DefaultBatchSize controls how many contract IDs are materialized per
///   eligibility query. Batches advance via a keyset cursor: each eligibility
///   query returns IDs strictly greater than the last ID of the previous batch
///   (ordered by Id). Because generation is idempotent and eligibility ("is
///   Active") does not change on success, the cursor is what drains the loop;
///   every eligible contract is re-attempted on every sweep.
///
/// FAILURE ISOLATION:
///   Platform enumeration failure → job-level exception (propagates to Hangfire).
///   Company-level failure         → log error, skip company, continue next.
///   Per-contract failure          → log warning, add to poison list, continue.
///   Poison threshold exceeded     → log error, abandon company, continue next.
/// </summary>
public class GenerateScheduledInstallmentsJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBusinessClock _businessClock;
    private readonly ILogger<GenerateScheduledInstallmentsJob> _logger;

    public const int DefaultBatchSize = 100;

    /// <summary>
    /// Maximum number of individual contract failures allowed per company per sweep.
    /// Once exceeded, the remaining batches for that company are abandoned — a sweep
    /// drowning in failures almost certainly has a systemic cause. The poison set is
    /// job-side memory only; the next scheduled sweep starts fresh.
    /// </summary>
    public const int MaxPoisonContractsPerCompany = 50;

    public GenerateScheduledInstallmentsJob(
        IServiceProvider serviceProvider,
        IBusinessClock businessClock,
        ILogger<GenerateScheduledInstallmentsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _businessClock = businessClock;
        _logger = logger;
    }

    /// <summary>
    /// Executes the full multi-tenant installment generation sweep.
    /// </summary>
    /// <param name="asOf">
    /// Optional override for the reference UTC instant (used for logging only —
    /// generation eligibility is purely "contract is Active").
    /// </param>
    /// <param name="batchSize">Number of contract IDs to retrieve per eligibility query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total number of contracts for which generation succeeded across all companies.</returns>
    public async Task<int> ExecuteSweepAsync(
        DateTimeOffset? asOf = null,
        int batchSize = DefaultBatchSize,
        CancellationToken cancellationToken = default)
    {
        var effectiveAsOf = asOf ?? _businessClock.UtcNow;
        var jordanBusinessDate = _businessClock.GetJordanBusinessDate(effectiveAsOf);

        _logger.LogInformation(
            "Starting multi-tenant scheduled installment generation sweep for Jordan business date {JordanDate} (UTC instant: {UtcNow}).",
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
            "Found {CompanyCount} active companies for scheduled installment generation.",
            companyIds.Count);

        int totalSuccessfullyGenerated = 0;

        foreach (var companyId in companyIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var companyGeneratedCount = await ProcessCompanyInstallmentGenerationAsync(
                    companyId, batchSize, cancellationToken);
                totalSuccessfullyGenerated += companyGeneratedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Company-level failure during installment generation sweep for company {CompanyId}. " +
                    "Skipping this company; remaining companies will continue.",
                    companyId);
            }
        }

        _logger.LogInformation(
            "Completed multi-tenant scheduled installment generation sweep. Total contracts processed: {TotalGenerated}.",
            totalSuccessfullyGenerated);

        return totalSuccessfullyGenerated;
    }

    private async Task<int> ProcessCompanyInstallmentGenerationAsync(
        Guid companyId,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int companyGeneratedCount = 0;
        Guid? afterContractId = null;
        var poisonContractIds = new HashSet<Guid>();

        while (!cancellationToken.IsCancellationRequested)
        {
            // -----------------------------------------------------------------
            // Step 2: Eligibility query — enumerate active contract IDs.
            //
            // TRANSACTION REQUIREMENT:
            //   Same as platform enumeration above. We MUST open a transaction
            //   before querying so TenantSessionInterceptor fires and sets:
            //     SET LOCAL app.current_company_id = '<companyId>'
            //   Without this, RLS returns 0 rows silently.
            //
            // A fresh scope is created per batch to prevent DbContext change
            // tracker accumulation and to ensure clean tenant context isolation.
            //
            // Eligibility ("is Active") does not change after a successful
            // generation, so the keyset cursor (Id > afterContractId) is what
            // advances the batches.
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

                await using var tx = await dbContext.BeginTransactionAsync(cancellationToken);
                // ↑ TenantSessionInterceptor fires:
                //   SET LOCAL app.current_company_id = '<companyId>'
                //   SET LOCAL app.is_platform_admin = 'false'

                var contractRepo = eligibilityScope.ServiceProvider
                    .GetRequiredService<ILeaseContractRepository>();

                batchContractIds = await contractRepo.GetActiveContractIdsAsync(
                    batchSize, afterContractId, cancellationToken);

                await tx.CommitAsync(cancellationToken);
            }

            if (batchContractIds.Count == 0)
                break;

            // Keyset cursor: results are ordered by Id, so the last returned ID is
            // the cursor for the next batch.
            afterContractId = batchContractIds[^1];

            _logger.LogInformation(
                "Company {CompanyId}: Found batch of {BatchCount} active lease contracts for installment generation.",
                companyId, batchContractIds.Count);

            // -----------------------------------------------------------------
            // Step 3: Generate installments for each contract individually.
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
                    return companyGeneratedCount;
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
                        new GenerateScheduledInstallmentsCommand(contractId),
                        cancellationToken);
                    companyGeneratedCount++;
                }
                catch (Exception ex)
                {
                    poisonContractIds.Add(contractId);
                    _logger.LogWarning(
                        ex,
                        "Failed to generate scheduled installments for lease contract {ContractId} in company {CompanyId}. " +
                        "Contract added to poison exclusion list for this sweep " +
                        "(poison count: {PoisonCount}/{PoisonThreshold}).",
                        contractId, companyId,
                        poisonContractIds.Count, MaxPoisonContractsPerCompany);
                }
            }
        }

        return companyGeneratedCount;
    }
}
