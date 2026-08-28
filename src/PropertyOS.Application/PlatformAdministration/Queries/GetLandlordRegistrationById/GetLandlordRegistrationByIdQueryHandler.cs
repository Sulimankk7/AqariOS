using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;

namespace PropertyOS.Application.PlatformAdministration.Queries.GetLandlordRegistrationById;

public sealed class GetLandlordRegistrationByIdQueryHandler
    : IRequestHandler<GetLandlordRegistrationByIdQuery, LandlordRegistrationDetailDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetLandlordRegistrationByIdQueryHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<LandlordRegistrationDetailDto> Handle(
        GetLandlordRegistrationByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsPlatformAdmin)
            throw new UnauthorizedAccessException("Platform administrator scope is required.");

        var result = await _dbContext.LandlordRegistrations
            .AsNoTracking()
            .Where(x => x.Id == request.RegistrationId)
            .Select(x => new LandlordRegistrationDetailDto
            {
                RegistrationId = x.Id,
                UserName = x.User.FullName,
                Email = x.User.Email,
                Phone = x.User.Phone,
                CompanyName = x.Company.LegalName,
                CompanyDisplayName = x.Company.DisplayName,
                CompanyType = x.Company.CompanyType.ToString(),
                CountryCode = x.Company.CountryCode,
                RegistrationDate = x.SubmittedAt,
                Status = x.Status.ToString(),
                ReviewedAt = x.ReviewedAt,
                RejectionReason = x.RejectionReason
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result ?? throw new NotFoundException("Landlord registration was not found.");
    }
}
