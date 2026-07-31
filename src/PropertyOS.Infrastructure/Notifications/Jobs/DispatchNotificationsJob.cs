using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies;
using PropertyOS.Application.Notifications;
using PropertyOS.Application.Notifications.Commands.DispatchNotification;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Infrastructure.Identity;

namespace PropertyOS.Infrastructure.Notifications.Jobs;

/// <summary>
/// Background job that sweeps dispatchable notifications (Pending, or with Failed
/// deliveries still under the retry cap) and sends each one through
/// DispatchNotificationCommand. The per-notification handler owns all delivery and
/// state-machine logic, so the sweep is orchestration-only and idempotent.
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
///   After MaxPoisonNotificationsPerCompany dispatch failures for a single company,
///   the remaining batches for that company are abandoned. The poison set stays
///   job-side (in memory) only — it is never sent to SQL.
///
/// BATCH SIZE / CURSOR:
///   DefaultBatchSize controls how many notification IDs are materialized per
///   eligibility query. Batches advance via a keyset cursor: each eligibility query
///   returns IDs strictly greater than the last ID of the previous batch (ordered by
///   Id), so the loop drains even though a successfully dispatched notification does
///   NOT always stop matching the eligibility predicate (its deliveries may have
///   failed non-terminally, keeping it retry-eligible). Non-terminal failures are
///   retried on the NEXT sweep, giving one attempt per delivery per sweep up to the cap.
///
/// FAILURE ISOLATION:
///   Platform enumeration failure  → job-level exception (propagates to Hangfire).
///   Company-level failure         → log error, skip company, continue next.
///   Per-notification failure      → log warning, add to poison list, continue.
///   Poison threshold exceeded     → log error, abandon company, continue next.
/// </summary>
public class DispatchNotificationsJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DispatchNotificationsJob> _logger;

    public const int DefaultBatchSize = 100;

    /// <summary>
    /// Maximum number of individual notification dispatch failures allowed per company
    /// per sweep. Once exceeded, the remaining batches for that company are abandoned —
    /// a sweep drowning in failures almost certainly has a systemic cause. The poison
    /// set is job-side memory only; the next scheduled sweep starts fresh.
    /// </summary>
    public const int MaxPoisonNotificationsPerCompany = 50;

    public DispatchNotificationsJob(
        IServiceProvider serviceProvider,
        ILogger<DispatchNotificationsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the full multi-tenant notification dispatch sweep.
    /// </summary>
    /// <param name="batchSize">Number of notification IDs to retrieve per eligibility query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total number of successfully dispatched notifications across all companies.</returns>
    public async Task<int> ExecuteSweepAsync(
        int batchSize = DefaultBatchSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting multi-tenant notification dispatch sweep.");

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
            "Found {CompanyCount} active companies for notification dispatch processing.",
            companyIds.Count);

        int totalSuccessfullyDispatched = 0;

        foreach (var companyId in companyIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var companyDispatchedCount = await ProcessCompanyNotificationsAsync(
                    companyId, batchSize, cancellationToken);
                totalSuccessfullyDispatched += companyDispatchedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Company-level failure during notification dispatch sweep for company {CompanyId}. " +
                    "Skipping this company; remaining companies will continue.",
                    companyId);
            }
        }

        _logger.LogInformation(
            "Completed multi-tenant notification dispatch sweep. Total notifications dispatched: {TotalDispatched}.",
            totalSuccessfullyDispatched);

        return totalSuccessfullyDispatched;
    }

    private async Task<int> ProcessCompanyNotificationsAsync(
        Guid companyId,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int companyDispatchedCount = 0;
        Guid? afterNotificationId = null;
        var poisonNotificationIds = new HashSet<Guid>();

        while (!cancellationToken.IsCancellationRequested)
        {
            // -----------------------------------------------------------------
            // Step 2: Eligibility query — enumerate dispatch candidate notification IDs.
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
            List<Guid> batchNotificationIds;
            using (var eligibilityScope = _serviceProvider.CreateScope())
            {
                var tenantSetter = eligibilityScope.ServiceProvider
                    .GetRequiredService<ISystemTenantContextSetter>();
                tenantSetter.SetCompanyScope(companyId);
                // ↑ Must happen BEFORE BeginTransactionAsync

                var dbContext = eligibilityScope.ServiceProvider
                    .GetRequiredService<IApplicationDbContext>();

                batchNotificationIds = await dbContext.ExecuteInTransactionAsync(async (ct) =>
                {
                    var notificationRepo = eligibilityScope.ServiceProvider
                        .GetRequiredService<INotificationRepository>();

                    return await notificationRepo.GetDispatchCandidateIdsAsync(
                        batchSize, afterNotificationId, ct);
                }, cancellationToken);
            }

            if (batchNotificationIds.Count == 0)
                break;

            // Keyset cursor: results are ordered by Id, so the last returned ID is
            // the cursor for the next batch — the loop strictly advances regardless
            // of per-item success or failure.
            afterNotificationId = batchNotificationIds[^1];

            _logger.LogInformation(
                "Company {CompanyId}: Found batch of {BatchCount} dispatch candidate notifications.",
                companyId, batchNotificationIds.Count);

            // -----------------------------------------------------------------
            // Step 3: Dispatch each notification individually.
            //
            // Each notification gets its own DI scope so that:
            //   - DbContext change tracker is fresh per command.
            //   - Tenant context is independently established.
            //   - TransactionBehavior manages the transaction for each command.
            //
            // Poison threshold: if failures accumulate beyond the threshold,
            // we abort this company's processing — a systemic cause is far more
            // likely than dozens of independent per-notification defects.
            // -----------------------------------------------------------------
            foreach (var notificationId in batchNotificationIds)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (poisonNotificationIds.Count >= MaxPoisonNotificationsPerCompany)
                {
                    _logger.LogError(
                        "Company {CompanyId}: Poison threshold of {Threshold} failed notifications reached. " +
                        "Abandoning remaining batches for this company. Failed notification IDs will be retried on the next sweep.",
                        companyId, MaxPoisonNotificationsPerCompany);
                    return companyDispatchedCount;
                }

                // Client-side poison skip: the cursor normally advances past poison
                // IDs, but never re-attempt one within the same sweep.
                if (poisonNotificationIds.Contains(notificationId))
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
                        new DispatchNotificationCommand(notificationId),
                        cancellationToken);
                    companyDispatchedCount++;
                }
                catch (Exception ex)
                {
                    poisonNotificationIds.Add(notificationId);
                    _logger.LogWarning(
                        ex,
                        "Failed to dispatch notification {NotificationId} for company {CompanyId}. " +
                        "Notification added to poison exclusion list for this sweep " +
                        "(poison count: {PoisonCount}/{PoisonThreshold}).",
                        notificationId, companyId,
                        poisonNotificationIds.Count, MaxPoisonNotificationsPerCompany);
                }
            }
        }

        return companyDispatchedCount;
    }
}
