using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies;
using PropertyOS.Application.UtilityBills;
using PropertyOS.Application.UtilityBills.Commands.SyncUtilityAccount;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Infrastructure.Identity;

namespace PropertyOS.Infrastructure.UtilityBills.Jobs;

/// <summary>
/// Multi-tenant Hangfire job that processes electricity billing accounts.
///
/// Triggered by 3 cron registrations in Program.cs:
///   "0 7  1-3 * *"  (Amman TZ) — Day 1–3 at 07:00
///   "0 9  1-3 * *"  (Amman TZ) — Day 1–3 at 09:00
///   "0 11 1-3 * *"  (Amman TZ) — Day 1–3 at 11:00
///
/// Each trigger processes only accounts WHERE next_check_at &lt;= now().
/// Most days of the month no accounts are due (next_check_at is set to the
/// 1st of next month), so this job returns in milliseconds with zero work.
///
/// CRITICAL ORDERING (per AqariOS multi-tenant job architecture):
///   1. Resolve ISystemTenantContextSetter from scope
///   2. Call SetCompanyScope / SetPlatformAdminScope         ← BEFORE BeginTransactionAsync
///   3. Then open transaction / call ISender
///   Any reordering breaks RLS.
/// </summary>
public sealed class CheckElectricityBillsJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBusinessClock _clock;
    private readonly ILogger<CheckElectricityBillsJob> _logger;

    public CheckElectricityBillsJob(
        IServiceProvider serviceProvider,
        IBusinessClock clock,
        ILogger<CheckElectricityBillsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _clock           = clock;
        _logger          = logger;
    }

    public async Task ExecuteBatchAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        _logger.LogInformation(
            "CheckElectricityBillsJob starting at {UtcNow}.", now);

        // ── Step 1: Enumerate active companies ──────────────────────────────
        List<Guid> companyIds;
        using (var platformScope = _serviceProvider.CreateScope())
        {
            var tenantSetter = platformScope.ServiceProvider
                .GetRequiredService<ISystemTenantContextSetter>();
            tenantSetter.SetPlatformAdminScope(); // BEFORE BeginTransactionAsync

            var db = platformScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            companyIds = await db.ExecuteInTransactionAsync(async ct =>
            {
                var companyRepo = platformScope.ServiceProvider
                    .GetRequiredService<ICompanyRepository>();
                return await companyRepo.GetActiveCompanyIdsAsync(ct);
            }, cancellationToken);
        }

        // ── Step 2: Process each company ────────────────────────────────────
        int batchSize;
        using (var optScope = _serviceProvider.CreateScope())
        {
            var opts = optScope.ServiceProvider
                .GetRequiredService<IOptionsSnapshot<UtilityBillsOptions>>();
            batchSize = opts.Value.Electricity.BatchSize;
        }

        foreach (var companyId in companyIds)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                await ProcessCompanyAsync(companyId, now, batchSize, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CheckElectricityBillsJob: error processing company {CompanyId}. Skipping.",
                    companyId);
            }
        }

        _logger.LogInformation("CheckElectricityBillsJob completed.");
    }

    private async Task ProcessCompanyAsync(
        Guid companyId,
        DateTimeOffset asOf,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int interBatchDelayMs;
        using (var optScope = _serviceProvider.CreateScope())
        {
            var opts = optScope.ServiceProvider
                .GetRequiredService<IOptionsSnapshot<UtilityBillsOptions>>();
            interBatchDelayMs = opts.Value.Electricity.InterBatchDelayMs;
        }

        List<Guid> batchIds;
        using (var eligScope = _serviceProvider.CreateScope())
        {
            var tenantSetter = eligScope.ServiceProvider
                .GetRequiredService<ISystemTenantContextSetter>();
            tenantSetter.SetCompanyScope(companyId); // BEFORE BeginTransactionAsync

            var db = eligScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            batchIds = await db.ExecuteInTransactionAsync(async ct =>
            {
                var repo = eligScope.ServiceProvider
                    .GetRequiredService<IUtilityAccountRepository>();
                return await repo.GetDueAccountIdsAsync(
                    UtilityType.Electricity, asOf, batchSize, afterId: null, ct);
            }, cancellationToken);
        }

        foreach (var accountId in batchIds)
        {
            if (cancellationToken.IsCancellationRequested) break;

            using var itemScope = _serviceProvider.CreateScope();
            var tenantSetter = itemScope.ServiceProvider
                .GetRequiredService<ISystemTenantContextSetter>();
            tenantSetter.SetCompanyScope(companyId); // BEFORE ISender.Send

            var sender = itemScope.ServiceProvider.GetRequiredService<ISender>();
            try
            {
                await sender.Send(
                    new SyncUtilityAccountCommand(accountId, IsBootstrapRun: false),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "CheckElectricityBillsJob: error syncing account {AccountId} in company {CompanyId}.",
                    accountId, companyId);
            }

            if (interBatchDelayMs > 0)
                await Task.Delay(interBatchDelayMs, cancellationToken);
        }
    }
}
