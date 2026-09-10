using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.PlatformAdministration.DTOs;

namespace PropertyOS.Application.PlatformAdministration.Queries.GetPlatformAdministrators;

internal sealed class GetPlatformAdministratorsQueryHandler
    : IRequestHandler<GetPlatformAdministratorsQuery, IReadOnlyList<PlatformAdministratorDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetPlatformAdministratorsQueryHandler(
        IApplicationDbContext dbContext,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<PlatformAdministratorDto>> Handle(
        GetPlatformAdministratorsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsPlatformAdmin)
            throw new UnauthorizedAccessException("Platform administrator scope is required.");

        return await _dbContext.UserSystemRoles
            .AsNoTracking()
            .Where(assignment =>
                assignment.Role.Code == PlatformRoles.SystemAdmin &&
                assignment.Role.IsSystem &&
                assignment.Role.CompanyId == null &&
                assignment.Role.DeletedAt == null &&
                assignment.User.DeletedAt == null)
            .OrderBy(assignment => assignment.User.FullName)
            .ThenBy(assignment => assignment.User.Email)
            .Select(assignment => new PlatformAdministratorDto(
                assignment.User.Id,
                assignment.User.FullName,
                assignment.User.Email,
                assignment.User.IsActive,
                assignment.User.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
