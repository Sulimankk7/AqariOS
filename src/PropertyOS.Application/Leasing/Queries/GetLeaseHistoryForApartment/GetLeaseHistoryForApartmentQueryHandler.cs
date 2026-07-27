using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForApartment;

public class GetLeaseHistoryForApartmentQueryHandler : IRequestHandler<GetLeaseHistoryForApartmentQuery, List<LeaseContractDto>>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ITenantContext _tenantContext;

    public GetLeaseHistoryForApartmentQueryHandler(ILeaseContractRepository leaseContractRepository, ITenantContext tenantContext)
    {
        _leaseContractRepository = leaseContractRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<LeaseContractDto>> Handle(GetLeaseHistoryForApartmentQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return await _leaseContractRepository.GetHistoryByApartmentIdAsync(request.ApartmentId, companyId, request.PageSize, cancellationToken);
    }
}
