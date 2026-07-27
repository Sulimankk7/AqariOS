using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.SearchTenants;

public class SearchTenantsQueryHandler : IRequestHandler<SearchTenantsQuery, List<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public SearchTenantsQueryHandler(ITenantRepository tenantRepository, ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<TenantDto>> Handle(SearchTenantsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        // Company scoping happens in the repository predicate (queries may run without an
        // RLS-scoped transaction).
        return await _tenantRepository.SearchAsync(companyId, request.SearchTerm, cancellationToken);
    }
}
