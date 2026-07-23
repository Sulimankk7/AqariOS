using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Documents.Security;

public class ConfidentialDocumentRequirement : IAuthorizationRequirement { }

public class ConfidentialDocumentAuthorizationHandler : AuthorizationHandler<ConfidentialDocumentRequirement>
{
    private readonly PropertyOsDbContext _dbContext;

    public ConfidentialDocumentAuthorizationHandler(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ConfidentialDocumentRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;

        var companyIdClaim = context.User.FindFirst("company_id")?.Value;

        if (System.Guid.TryParse(userIdClaim, out var userId) && System.Guid.TryParse(companyIdClaim, out var companyId))
        {
            var hasPermission = await (from ucr in _dbContext.UserCompanyRoles.AsNoTracking()
                                       join rp in _dbContext.RolePermissions.AsNoTracking() on ucr.RoleId equals rp.RoleId
                                       join p in _dbContext.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                                       where ucr.UserId == userId && ucr.CompanyId == companyId && ucr.DeletedAt == null
                                             && p.Key == "documents.view_confidential"
                                       select p.Id).AnyAsync();

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }
}
