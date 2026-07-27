using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.SearchLeaseContracts;

public class SearchLeaseContractsQueryHandler : IRequestHandler<SearchLeaseContractsQuery, List<LeaseContractDto>>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ITenantContext _tenantContext;

    public SearchLeaseContractsQueryHandler(ILeaseContractRepository leaseContractRepository, ITenantContext tenantContext)
    {
        _leaseContractRepository = leaseContractRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<LeaseContractDto>> Handle(SearchLeaseContractsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return await _leaseContractRepository.SearchContractsAsync(request.SearchTerm, companyId, request.PageSize, cancellationToken);
    }
}
