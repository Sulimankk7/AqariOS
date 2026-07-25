using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Identity.Provisioning.Steps;

/// <summary>
/// Provisioning step 3: Persists User and Company entities so database-generated primary keys (Company.Id) are populated.
/// </summary>
public class SaveInitialTenantEntitiesStep : ITenantProvisioningStep
{
    private readonly IApplicationDbContext _dbContext;

    public SaveInitialTenantEntitiesStep(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public int Order => 30;

    public async Task ExecuteAsync(TenantProvisioningContext context, CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
