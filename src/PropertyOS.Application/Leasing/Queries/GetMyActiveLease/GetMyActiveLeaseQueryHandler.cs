using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Queries.GetMyActiveLease;

public class GetMyActiveLeaseQueryHandler : IRequestHandler<GetMyActiveLeaseQuery, TenantLeaseDto?>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetMyActiveLeaseQueryHandler(
        ITenantRepository tenantRepository,
        ILeaseContractRepository leaseContractRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _leaseContractRepository = leaseContractRepository ?? throw new ArgumentNullException(nameof(leaseContractRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    }

    public async Task<TenantLeaseDto?> Handle(GetMyActiveLeaseQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId ?? throw new InvalidOperationException("Authenticated user context is required.");

        // 1. Server-side identity resolution: lookup Tenant record linked to authenticated UserId within Company scope
        var tenant = await _tenantRepository.GetByUserIdAsync(companyId, userId, cancellationToken);
        if (tenant == null)
        {
            return null;
        }

        // 2. Query tenant's current active lease contract with DB-level projection and tenant isolation
        return await _leaseContractRepository.GetActiveLeaseByTenantIdAsync(tenant.Id, companyId, cancellationToken);
    }
}
