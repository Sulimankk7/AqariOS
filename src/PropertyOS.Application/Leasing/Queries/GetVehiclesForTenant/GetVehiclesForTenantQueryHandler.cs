using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetVehiclesForTenant;

public class GetVehiclesForTenantQueryHandler : IRequestHandler<GetVehiclesForTenantQuery, List<TenantVehicleDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public GetVehiclesForTenantQueryHandler(ITenantRepository tenantRepository, ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<TenantVehicleDto>> Handle(GetVehiclesForTenantQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null || tenant.CompanyId != companyId)
        {
            throw new NotFoundException($"Tenant with ID {request.TenantId} was not found.");
        }

        return await _tenantRepository.GetVehiclesForTenantAsync(request.TenantId, companyId, cancellationToken);
    }
}
