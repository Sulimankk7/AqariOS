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
using PropertyOS.Application.Financials.Commands.MarkRentPaymentOverdue;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Infrastructure.Identity;

namespace PropertyOS.Infrastructure.Financials.Jobs;

/// <summary>
/// Background job that flips Pending scheduled installments whose due date has passed
/// (relative to the Jordan business calendar: Asia/Amman) to their derived overdue
/// settlement status via MarkRentPaymentOverdueCommand. The per-payment handler locks
/// the row (FOR UPDATE) and re-derives the status, so the sweep is idempotent and
/// safe against concurrent allocation writers.
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
///   After MaxPoisonPaymentsPerCompany payment failures for a single company,
///   the remaining batches for that company are abandoned. The poison set stays
///   job-side (in memory) only — it is never sent to SQL.
///
/// BATCH SIZE / CURSOR:
///   DefaultBatchSize controls how many payment IDs are materialized per
///   eligibility query. Batches advance via a keyset cursor: each eligibility
///   query returns IDs strictly greater than the last ID of the previous batch
///   (ordered by Id), so the loop strictly advances regardless of per-item
///   success or failure.
///
/// FAILURE ISOLATION:
///   Platform enumeration failure → job-level exception (propagates to Hangfire).
///   Company-level failure         → log error, skip company, continue next.
///   Per-payment failure           → log warning, add to poison list, continue.
///   Poison threshold exceeded     → log error, abandon company, continue next.
/// </summary>
public class MarkOverdueRentPaymentsJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBusinessClock _businessClock;
    private readonly ILogger<MarkOverdueRentPaymentsJob> _logger;

    public const int DefaultBatchSize = 100;

    /// <summary>
    /// Maximum number of individual payment failures allowed per company per sweep.
    /// Once exceeded, the remaining batches for that company are abandoned — a sweep
    /// drowning in failures almost certainly has a systemic cause. The poison set is
    /// job-side memory only; the next scheduled sweep starts fresh.
    /// </summary>
    public const int MaxPoisonPaymentsPerCompany = 50;

    public MarkOverdueRentPaymentsJob(
        IServiceProvider serviceProvider,
        IBusinessClock businessClock,
        ILogger<MarkOverdueRentPaymentsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _businessClock = businessClock;
        _logger = logger;
    }

    /// <summary>
    /// Executes the full multi-tenant overdue-payment sweep.
    /// </summary>
    /// <param name="asOf">
    /// Optional override for the reference UTC instant. When null, uses the
    /// current wall-clock UTC time. The same instant is used to derive the
    /// Jordan business date (for eligibility) and is forwarded to every
    /// dispatched command (for consistent status derivation). No multiple
    /// wall-clock reads occur per sweep.
    /// </param>
    /// <param name="batchSize">Number of payment IDs to retrieve per eligibility query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total number of successfully processed payments across all companies.</returns>
    public async Task<int> ExecuteSweepAsync(
        DateTimeOffset? asOf = null,
        int batchSize = DefaultBatchSize,
        CancellationToken cancellationToken = default)
    {
        var effectiveAsOf = asOf ?? _businessClock.UtcNow;
        var jordanBusinessDate = _businessClock.GetJordanBusinessDate(effectiveAsOf);

        _logger.LogInformation(
            "Starting multi-tenant overdue rent payment sweep for Jordan business date {JordanDate} (UTC instant: {UtcNow}).",
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
            "Found {CompanyCount} active companies for overdue rent payment processing.",
            companyIds.Count);

        int totalSuccessfullyMarked = 0;

        foreach (var companyId in companyIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var companyMarkedCount = await ProcessCompanyOverduePaymentsAsync(
                    companyId, jordanBusinessDate, effectiveAsOf, batchSize, cancellationToken);
                totalSuccessfullyMarked += companyMarkedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Company-level failure during overdue rent payment sweep for company {CompanyId}. " +
                    "Skipping this company; remaining companies will continue.",
                    companyId);
            }
        }

        _logger.LogInformation(
            "Completed multi-tenant overdue rent payment sweep. Total payments processed: {TotalMarked}.",
            totalSuccessfullyMarked);

        return totalSuccessfullyMarked;
    }

    private async Task<int> ProcessCompanyOverduePaymentsAsync(
        Guid companyId,
        DateOnly jordanBusinessDate,
        DateTimeOffset effectiveAsOf,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int companyMarkedCount = 0;
        Guid? afterPaymentId = null;
        var poisonPaymentIds = new HashSet<Guid>();

        while (!cancellationToken.IsCancellationRequested)
        {
            // -----------------------------------------------------------------
            // Step 2: Eligibility query — enumerate overdue candidate payment IDs.
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
            List<Guid> batchPaymentIds;
            using (var eligibilityScope = _serviceProvider.CreateScope())
            {
                var tenantSetter = eligibilityScope.ServiceProvider
                    .GetRequiredService<ISystemTenantContextSetter>();
                tenantSetter.SetCompanyScope(companyId);
                // ↑ Must happen BEFORE BeginTransactionAsync

                var dbContext = eligibilityScope.ServiceProvider
                    .GetRequiredService<IApplicationDbContext>();

                batchPaymentIds = await dbContext.ExecuteInTransactionAsync(async (ct) =>
                {
                    var rentPaymentRepo = eligibilityScope.ServiceProvider
                        .GetRequiredService<IRentPaymentRepository>();

                    return await rentPaymentRepo.GetOverdueCandidateIdsAsync(
                        jordanBusinessDate, batchSize, afterPaymentId, ct);
                }, cancellationToken);
            }

            if (batchPaymentIds.Count == 0)
                break;

            // Keyset cursor: results are ordered by Id, so the last returned ID is
            // the cursor for the next batch.
            afterPaymentId = batchPaymentIds[^1];

            _logger.LogInformation(
                "Company {CompanyId}: Found batch of {BatchCount} overdue candidate rent payments.",
                companyId, batchPaymentIds.Count);

            // -----------------------------------------------------------------
            // Step 3: Mark each payment individually.
            //
            // Each payment gets its own DI scope so that:
            //   - DbContext change tracker is fresh per command.
            //   - Tenant context is independently established.
            //   - TransactionBehavior manages the transaction for each command.
            //
            // Poison threshold: if failures accumulate beyond the threshold,
            // we abort this company's processing — a systemic cause is far more
            // likely than dozens of independent per-payment defects.
            // -----------------------------------------------------------------
            foreach (var paymentId in batchPaymentIds)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (poisonPaymentIds.Count >= MaxPoisonPaymentsPerCompany)
                {
                    _logger.LogError(
                        "Company {CompanyId}: Poison threshold of {Threshold} failed payments reached. " +
                        "Abandoning remaining batches for this company. Failed payment IDs will be retried on the next sweep.",
                        companyId, MaxPoisonPaymentsPerCompany);
                    return companyMarkedCount;
                }

                // Client-side poison skip: the cursor normally advances past poison
                // IDs, but never re-attempt one within the same sweep.
                if (poisonPaymentIds.Contains(paymentId))
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
                        new MarkRentPaymentOverdueCommand(paymentId, effectiveAsOf),
                        cancellationToken);
                    companyMarkedCount++;
                }
                catch (Exception ex)
                {
                    poisonPaymentIds.Add(paymentId);
                    _logger.LogWarning(
                        ex,
                        "Failed to mark rent payment {PaymentId} overdue for company {CompanyId}. " +
                        "Payment added to poison exclusion list for this sweep " +
                        "(poison count: {PoisonCount}/{PoisonThreshold}).",
                        paymentId, companyId,
                        poisonPaymentIds.Count, MaxPoisonPaymentsPerCompany);
                }
            }
        }

        return companyMarkedCount;
    }
}
