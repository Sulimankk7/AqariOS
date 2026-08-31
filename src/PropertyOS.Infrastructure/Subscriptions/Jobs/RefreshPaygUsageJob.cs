using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies;
using PropertyOS.Application.Subscriptions.UseCases;
using PropertyOS.Infrastructure.Identity;

namespace PropertyOS.Infrastructure.Subscriptions.Jobs;

/// <summary>Daily, company-isolated PAYG metering and period-finalization sweep.</summary>
public sealed class RefreshPaygUsageJob
{
    private readonly IServiceProvider _services;
    private readonly IBusinessClock _clock;
    private readonly ILogger<RefreshPaygUsageJob> _logger;

    public RefreshPaygUsageJob(IServiceProvider services, IBusinessClock clock, ILogger<RefreshPaygUsageJob> logger)
    {
        _services = services;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> ExecuteSweepAsync(DateTimeOffset? asOf = null, CancellationToken cancellationToken = default)
    {
        var instant = asOf ?? _clock.UtcNow;
        List<Guid> companyIds;
        using (var scope = _services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ISystemTenantContextSetter>().SetPlatformAdminScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            companyIds = await db.ExecuteInTransactionAsync(async ct =>
                await scope.ServiceProvider.GetRequiredService<ICompanyRepository>().GetActiveCompanyIdsAsync(ct), cancellationToken);
        }

        var refreshed = 0;
        foreach (var companyId in companyIds)
        {
            if (cancellationToken.IsCancellationRequested) break;
            using var scope = _services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ISystemTenantContextSetter>().SetCompanyScope(companyId);
            try
            {
                await scope.ServiceProvider.GetRequiredService<ISender>()
                    .Send(new GetCurrentPaygUsageQuery(instant), cancellationToken);
                refreshed++;
            }
            catch (PropertyOS.Application.Common.Exceptions.NotFoundException)
            {
                // A company without a subscription has no PAYG work.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PAYG usage refresh failed for CompanyId={CompanyId}.", companyId);
            }
        }

        return refreshed;
    }
}
