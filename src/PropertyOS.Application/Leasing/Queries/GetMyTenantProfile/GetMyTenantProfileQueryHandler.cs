using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetMyTenantProfile;

public class GetMyTenantProfileQueryHandler : IRequestHandler<GetMyTenantProfileQuery, TenantDetailDto?>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetMyTenantProfileQueryHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    }

    public async Task<TenantDetailDto?> Handle(GetMyTenantProfileQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId ?? throw new InvalidOperationException("Authenticated user context is required.");

        // 1. Resolve Tenant aggregate linked to current UserId within Company scope
        var tenant = await _tenantRepository.GetByUserIdAsync(companyId, userId, cancellationToken);
        if (tenant == null)
        {
            return null;
        }

        // 2. Return detail projection including child collections
        return await _tenantRepository.GetDetailByIdAsync(tenant.Id, companyId, cancellationToken);
    }
}
