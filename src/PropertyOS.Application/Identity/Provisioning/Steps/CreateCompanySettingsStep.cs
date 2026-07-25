using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Companies;

namespace PropertyOS.Application.Identity.Provisioning.Steps;

/// <summary>
/// Provisioning step 4: Creates default <see cref="CompanySettings"/> for the newly provisioned Company.
/// </summary>
public class CreateCompanySettingsStep : ITenantProvisioningStep
{
    private readonly IApplicationDbContext _dbContext;

    public CreateCompanySettingsStep(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public int Order => 40;

    public Task ExecuteAsync(TenantProvisioningContext context, CancellationToken cancellationToken)
    {
        var company = context.Company ?? throw new InvalidOperationException("Company entity must be populated before creating CompanySettings.");

        var settings = CompanySettings.CreateWithDefaults(company.Id, context.CreatedAt);
        _dbContext.CompanySettings.Add(settings);
        context.CompanySettings = settings;

        return Task.CompletedTask;
    }
}
