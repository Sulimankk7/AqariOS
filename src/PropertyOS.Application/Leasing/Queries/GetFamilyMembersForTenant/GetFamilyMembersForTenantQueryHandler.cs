using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetFamilyMembersForTenant;

public class GetFamilyMembersForTenantQueryHandler : IRequestHandler<GetFamilyMembersForTenantQuery, List<TenantFamilyMemberDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public GetFamilyMembersForTenantQueryHandler(ITenantRepository tenantRepository, ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<TenantFamilyMemberDto>> Handle(GetFamilyMembersForTenantQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null || tenant.CompanyId != companyId)
        {
            throw new NotFoundException($"Tenant with ID {request.TenantId} was not found.");
        }

        return await _tenantRepository.GetFamilyMembersForTenantAsync(request.TenantId, companyId, cancellationToken);
    }
}
