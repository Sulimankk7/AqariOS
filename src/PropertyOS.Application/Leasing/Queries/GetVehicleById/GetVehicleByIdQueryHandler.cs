using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetVehicleById;

public class GetVehicleByIdQueryHandler : IRequestHandler<GetVehicleByIdQuery, TenantVehicleDto?>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public GetVehicleByIdQueryHandler(ITenantRepository tenantRepository, ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<TenantVehicleDto?> Handle(GetVehicleByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        return await _tenantRepository.GetVehicleDtoByIdAsync(request.TenantId, request.VehicleId, companyId, cancellationToken);
    }
}
