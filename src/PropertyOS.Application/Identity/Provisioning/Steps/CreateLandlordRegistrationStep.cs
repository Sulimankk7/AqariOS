using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Application.Identity.Provisioning.Steps;

public sealed class CreateLandlordRegistrationStep : ITenantProvisioningStep
{
    private readonly IApplicationDbContext _dbContext;

    public CreateLandlordRegistrationStep(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public int Order => 65;

    public Task ExecuteAsync(TenantProvisioningContext context, CancellationToken cancellationToken)
    {
        var user = context.User ?? throw new InvalidOperationException("User must be created before registration.");
        var company = context.Company ?? throw new InvalidOperationException("Company must be created before registration.");
        var membership = context.UserCompanyRole ?? throw new InvalidOperationException("Membership must be created before registration.");

        var registration = LandlordRegistration.CreatePending(
            user.Id,
            company.Id,
            membership.Id,
            context.CreatedAt);

        _dbContext.LandlordRegistrations.Add(registration);
        context.Registration = registration;
        return Task.CompletedTask;
    }
}
