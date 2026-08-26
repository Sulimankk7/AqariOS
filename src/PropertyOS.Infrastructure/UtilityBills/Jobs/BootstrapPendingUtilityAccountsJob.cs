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
/// Bounded recovery sweep for accounts whose initial bootstrap did not complete.
/// This covers provider-disabled periods and lost/failed enqueue attempts without
/// making unbootstrapped accounts visible to the normal due-account scheduler.
/// </summary>
public sealed class BootstrapPendingUtilityAccountsJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BootstrapPendingUtilityAccountsJob> _logger;

    public BootstrapPendingUtilityAccountsJob(
        IServiceProvider serviceProvider,
        ILogger<BootstrapPendingUtilityAccountsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteBatchAsync(CancellationToken cancellationToken)
    {
        List<Guid> companyIds;
        using (var scope = _serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ISystemTenantContextSetter>()
                .SetPlatformAdminScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            companyIds = await db.ExecuteInTransactionAsync(async ct =>
                await scope.ServiceProvider.GetRequiredService<ICompanyRepository>()
                    .GetActiveCompanyIdsAsync(ct), cancellationToken);
        }

        foreach (var companyId in companyIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessCompanyTypeAsync(companyId, UtilityType.Electricity, cancellationToken);
            await ProcessCompanyTypeAsync(companyId, UtilityType.Water, cancellationToken);
        }
    }

    private async Task ProcessCompanyTypeAsync(
        Guid companyId,
        UtilityType utilityType,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ISystemTenantContextSetter>()
            .SetCompanyScope(companyId);

        var options = scope.ServiceProvider
            .GetRequiredService<IOptionsSnapshot<UtilityBillsOptions>>().Value;
        var batchSize = utilityType == UtilityType.Electricity
            ? options.Electricity.BatchSize
            : options.Water.BatchSize;

        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var accountIds = await db.ExecuteInTransactionAsync(async ct =>
            await scope.ServiceProvider.GetRequiredService<IUtilityAccountRepository>()
                .GetPendingBootstrapIdsAsync(utilityType, batchSize, afterId: null, ct),
            cancellationToken);

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        foreach (var accountId in accountIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await sender.Send(
                    new SyncUtilityAccountCommand(accountId, IsBootstrapRun: true),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Bootstrap recovery failed for account {AccountId} in company {CompanyId}.",
                    accountId, companyId);
            }
        }
    }
}
