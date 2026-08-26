using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills;
using PropertyOS.Application.UtilityBills.Commands.SyncUtilityAccount;
using PropertyOS.Infrastructure.Identity;

namespace PropertyOS.Infrastructure.UtilityBills.Jobs;

/// <summary>
/// Durable, account-specific normal synchronization entry point used by management APIs.
/// Resolves the owning company before dispatching through the production MediatR pipeline.
/// </summary>
public sealed class SyncUtilityAccountJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncUtilityAccountJob> _logger;

    public SyncUtilityAccountJob(
        IServiceProvider serviceProvider,
        ILogger<SyncUtilityAccountJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid utilityAccountId, CancellationToken cancellationToken)
    {
        Guid? companyId;
        using (var platformScope = _serviceProvider.CreateScope())
        {
            platformScope.ServiceProvider
                .GetRequiredService<ISystemTenantContextSetter>()
                .SetPlatformAdminScope();

            var db = platformScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            companyId = await db.ExecuteInTransactionAsync(async ct =>
            {
                var repository = platformScope.ServiceProvider
                    .GetRequiredService<IUtilityAccountRepository>();
                return (await repository.GetByIdAsync(utilityAccountId, ct))?.CompanyId;
            }, cancellationToken);
        }

        if (companyId is null)
        {
            _logger.LogWarning(
                "SyncUtilityAccountJob: account {AccountId} was not found.",
                utilityAccountId);
            return;
        }

        using var companyScope = _serviceProvider.CreateScope();
        companyScope.ServiceProvider
            .GetRequiredService<ISystemTenantContextSetter>()
            .SetCompanyScope(companyId.Value);

        await companyScope.ServiceProvider.GetRequiredService<ISender>().Send(
            new SyncUtilityAccountCommand(utilityAccountId, IsBootstrapRun: false),
            cancellationToken);
    }
}
