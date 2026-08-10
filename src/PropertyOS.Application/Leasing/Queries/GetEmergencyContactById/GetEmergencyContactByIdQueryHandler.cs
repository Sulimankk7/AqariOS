using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetEmergencyContactById;

public class GetEmergencyContactByIdQueryHandler : IRequestHandler<GetEmergencyContactByIdQuery, TenantEmergencyContactDto?>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public GetEmergencyContactByIdQueryHandler(ITenantRepository tenantRepository, ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<TenantEmergencyContactDto?> Handle(GetEmergencyContactByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        return await _tenantRepository.GetEmergencyContactDtoByIdAsync(request.TenantId, request.ContactId, companyId, cancellationToken);
    }
}
