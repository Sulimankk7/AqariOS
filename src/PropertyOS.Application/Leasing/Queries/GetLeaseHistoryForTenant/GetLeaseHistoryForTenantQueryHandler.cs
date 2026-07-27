using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForTenant;

public class GetLeaseHistoryForTenantQueryHandler : IRequestHandler<GetLeaseHistoryForTenantQuery, List<LeaseContractDto>>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ITenantContext _tenantContext;

    public GetLeaseHistoryForTenantQueryHandler(ILeaseContractRepository leaseContractRepository, ITenantContext tenantContext)
    {
        _leaseContractRepository = leaseContractRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<LeaseContractDto>> Handle(GetLeaseHistoryForTenantQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return await _leaseContractRepository.GetHistoryByTenantIdAsync(request.TenantId, companyId, request.PageSize, cancellationToken);
    }
}
