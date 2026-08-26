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
/// Multi-tenant Hangfire job that processes water billing accounts.
///
/// Triggered by a single daily cron in Program.cs:
///   "0 6 * * *"  (Amman TZ) — every day at 06:00
///
/// The NextCheckAt guard eliminates 99%+ of accounts from consideration on
/// any given day. Only accounts whose estimated bill window has arrived
/// (next_check_at &lt;= now()) are returned by GetDueAccountIdsAsync.
///
/// CRITICAL ORDERING (per AqariOS multi-tenant job architecture):
///   SetCompanyScope MUST precede BeginTransactionAsync / ISender.Send.
/// </summary>
public sealed class CheckWaterBillsJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBusinessClock _clock;
    private readonly ILogger<CheckWaterBillsJob> _logger;

    public CheckWaterBillsJob(
        IServiceProvider serviceProvider,
        IBusinessClock clock,
        ILogger<CheckWaterBillsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _clock           = clock;
        _logger          = logger;
    }

    public async Task ExecuteBatchAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        _logger.LogInformation("CheckWaterBillsJob starting at {UtcNow}.", now);

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

        int batchSize;
        using (var optScope = _serviceProvider.CreateScope())
        {
            var opts = optScope.ServiceProvider
                .GetRequiredService<IOptionsSnapshot<UtilityBillsOptions>>();
            batchSize = opts.Value.Water.BatchSize;
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
                    "CheckWaterBillsJob: error processing company {CompanyId}. Skipping.",
                    companyId);
            }
        }

        _logger.LogInformation("CheckWaterBillsJob completed.");
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
            interBatchDelayMs = opts.Value.Water.InterBatchDelayMs;
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
                    UtilityType.Water, asOf, batchSize, afterId: null, ct);
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
                    "CheckWaterBillsJob: error syncing account {AccountId} in company {CompanyId}.",
                    accountId, companyId);
            }

            if (interBatchDelayMs > 0)
                await Task.Delay(interBatchDelayMs, cancellationToken);
        }
    }
}
