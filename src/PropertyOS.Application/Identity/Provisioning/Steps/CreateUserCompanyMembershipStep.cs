using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Application.Identity.Provisioning.Steps;

/// <summary>
/// Provisioning step 6: Creates the <see cref="UserCompanyRole"/> entity associating the founding User with the Company in Active status.
/// </summary>
public class CreateUserCompanyMembershipStep : ITenantProvisioningStep
{
    private readonly IApplicationDbContext _dbContext;

    public CreateUserCompanyMembershipStep(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public int Order => 60;

    public Task ExecuteAsync(TenantProvisioningContext context, CancellationToken cancellationToken)
    {
        var user = context.User ?? throw new InvalidOperationException("User entity must be populated before creating UserCompanyRole.");
        var company = context.Company ?? throw new InvalidOperationException("Company entity must be populated before creating UserCompanyRole.");
        var adminRole = context.AdminRole ?? throw new InvalidOperationException("AdminRole entity must be populated before creating UserCompanyRole.");

        var membership = new UserCompanyRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CompanyId = company.Id,
            RoleId = adminRole.Id,
            Status = MembershipStatus.InvitedPending,
            InvitedAt = context.CreatedAt,
            JoinedAt = null,
            CreatedAt = context.CreatedAt,
            UpdatedAt = context.CreatedAt
        };

        _dbContext.UserCompanyRoles.Add(membership);
        context.UserCompanyRole = membership;

        return Task.CompletedTask;
    }
}
