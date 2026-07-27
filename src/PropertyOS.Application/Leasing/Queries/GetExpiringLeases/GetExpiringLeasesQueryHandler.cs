using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.GetExpiringLeases;

public class GetExpiringLeasesQueryHandler : IRequestHandler<GetExpiringLeasesQuery, List<LeaseContractDto>>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ITenantContext _tenantContext;

    public GetExpiringLeasesQueryHandler(ILeaseContractRepository leaseContractRepository, ITenantContext tenantContext)
    {
        _leaseContractRepository = leaseContractRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<LeaseContractDto>> Handle(GetExpiringLeasesQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return await _leaseContractRepository.GetExpiringLeasesAsync(request.DaysAhead, companyId, cancellationToken);
    }
}
