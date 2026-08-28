using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Application.PlatformAdministration.Queries.GetPendingLandlordRegistrations;

public sealed class GetPendingLandlordRegistrationsQueryHandler
    : IRequestHandler<GetPendingLandlordRegistrationsQuery, LandlordRegistrationPageDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetPendingLandlordRegistrationsQueryHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<LandlordRegistrationPageDto> Handle(
        GetPendingLandlordRegistrationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsPlatformAdmin)
            throw new UnauthorizedAccessException("Platform administrator scope is required.");

        var query = _dbContext.LandlordRegistrations
            .AsNoTracking()
            .Where(x => x.Status == RegistrationApprovalStatus.Pending);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.SubmittedAt)
            .ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new LandlordRegistrationListItemDto
            {
                RegistrationId = x.Id,
                UserName = x.User.FullName,
                Email = x.User.Email,
                CompanyName = x.Company.LegalName,
                RegistrationDate = x.SubmittedAt,
                Status = x.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return new LandlordRegistrationPageDto
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
