using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Application.Identity.Provisioning.Steps;

/// <summary>
/// Provisioning step 5: Creates the initial Company Administrator <see cref="Role"/> for the tenant organization
/// and grants all active platform permissions via <see cref="RolePermission"/>.
/// </summary>
public class CreateCompanyAdminRoleStep : ITenantProvisioningStep
{
    private readonly IApplicationDbContext _dbContext;

    public CreateCompanyAdminRoleStep(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public int Order => 50;

    public async Task ExecuteAsync(TenantProvisioningContext context, CancellationToken cancellationToken)
    {
        var company = context.Company ?? throw new InvalidOperationException("Company entity must be populated before creating Admin Role.");

        var adminRole = new Role
        {
            Id = Guid.CreateVersion7(),
            CompanyId = company.Id,
            Code = "COMPANY_ADMIN",
            NameEn = "Company Administrator",
            NameAr = "مدير الشركة",
            IsSystem = false,
            CreatedAt = context.CreatedAt,
            UpdatedAt = context.CreatedAt
        };

        _dbContext.Roles.Add(adminRole);
        context.AdminRole = adminRole;

        var activePermissions = await _dbContext.Permissions
            .AsNoTracking()
            .Where(p => !p.IsDeprecated && !p.Key.StartsWith("platform."))
            .ToListAsync(cancellationToken);

        foreach (var perm in activePermissions)
        {
            _dbContext.RolePermissions.Add(new RolePermission
            {
                Id = Guid.CreateVersion7(),
                RoleId = adminRole.Id,
                PermissionId = perm.Id,
                GrantedAt = context.CreatedAt
            });
        }
    }
}
