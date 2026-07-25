using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Identity.Provisioning.Steps;

/// <summary>
/// Provisioning step 7: Persists CompanySettings, Role, and UserCompanyRole entities to the database.
/// </summary>
public class SaveProvisionedTenantEntitiesStep : ITenantProvisioningStep
{
    private readonly IApplicationDbContext _dbContext;

    public SaveProvisionedTenantEntitiesStep(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public int Order => 70;

    public async Task ExecuteAsync(TenantProvisioningContext context, CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
