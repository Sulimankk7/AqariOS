using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Commands.SyncUtilityAccount;
using PropertyOS.Infrastructure.Identity;

namespace PropertyOS.Infrastructure.UtilityBills.Jobs;

/// <summary>
/// Hangfire job that performs the one-time historical bill import for a newly
/// linked utility account.
///
/// Enqueued via IPostCommitRegistrar AFTER LinkUtilityAccountCommand commits.
/// This ensures the job is never enqueued if the transaction rolls back.
///
/// What it does:
///   1. Dispatches SyncUtilityAccountCommand(isBootstrapRun: true)
///      - Provider called with sinceDate = null → full history returned
///      - All bills stored with is_from_historical_backfill = true
///      - No notifications sent for historical bills
///      - Water interval statistics calculated from history (once only)
///      - historical_bootstrap_completed set to true → account enters scheduler index
///
/// After this job completes, the account is picked up by the normal
/// CheckElectricityBillsJob or CheckWaterBillsJob on the next scheduled trigger.
///
/// Idempotency: if the job fails and Hangfire retries, SyncUtilityAccountCommand
/// handles it safely:
///   - historical_bootstrap_completed still false → re-processes
///   - Bills inserted with ON CONFLICT DO NOTHING → no duplicates
///
/// CRITICAL ORDERING:
///   SetCompanyScope MUST precede ISender.Send (which triggers TransactionBehavior
///   → BeginTransactionAsync → TenantSessionInterceptor).
/// </summary>
public sealed class BootstrapUtilityAccountJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BootstrapUtilityAccountJob> _logger;

    public BootstrapUtilityAccountJob(
        IServiceProvider serviceProvider,
        ILogger<BootstrapUtilityAccountJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger          = logger;
    }

    /// <summary>
    /// Entry point called by Hangfire.
    /// The companyId is resolved from the account itself within a platform-admin scope.
    /// </summary>
    public async Task ExecuteAsync(Guid utilityAccountId, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "BootstrapUtilityAccountJob starting for account {AccountId}.", utilityAccountId);

        // Resolve companyId via platform-admin scope (the account belongs to exactly one company)
        Guid companyId;
        using (var platformScope = _serviceProvider.CreateScope())
        {
            var tenantSetter = platformScope.ServiceProvider
                .GetRequiredService<ISystemTenantContextSetter>();
            tenantSetter.SetPlatformAdminScope(); // BEFORE BeginTransactionAsync

            var db = platformScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var id = await db.ExecuteInTransactionAsync(async ct =>
            {
                var repo = platformScope.ServiceProvider
                    .GetRequiredService<Application.UtilityBills.IUtilityAccountRepository>();
                var account = await repo.GetByIdAsync(utilityAccountId, ct);
                return account?.CompanyId;
            }, cancellationToken);

            if (id is null)
            {
                _logger.LogWarning(
                    "BootstrapUtilityAccountJob: account {AccountId} not found. Skipping.",
                    utilityAccountId);
                return;
            }
            companyId = id.Value;
        }

        // Dispatch the sync command within the correct company scope
        using var itemScope = _serviceProvider.CreateScope();
        var itemTenantSetter = itemScope.ServiceProvider
            .GetRequiredService<ISystemTenantContextSetter>();
        itemTenantSetter.SetCompanyScope(companyId); // BEFORE ISender.Send

        var sender = itemScope.ServiceProvider.GetRequiredService<ISender>();
        try
        {
            await sender.Send(
                new SyncUtilityAccountCommand(utilityAccountId, IsBootstrapRun: true),
                cancellationToken);

            _logger.LogInformation(
                "BootstrapUtilityAccountJob completed for account {AccountId} in company {CompanyId}.",
                utilityAccountId, companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "BootstrapUtilityAccountJob: error bootstrapping account {AccountId} in company {CompanyId}.",
                utilityAccountId, companyId);
            throw; // Re-throw so Hangfire retries
        }
    }
}
