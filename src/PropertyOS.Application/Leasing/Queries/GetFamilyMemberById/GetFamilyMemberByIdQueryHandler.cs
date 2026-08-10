using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetFamilyMemberById;

public class GetFamilyMemberByIdQueryHandler : IRequestHandler<GetFamilyMemberByIdQuery, TenantFamilyMemberDto?>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public GetFamilyMemberByIdQueryHandler(ITenantRepository tenantRepository, ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<TenantFamilyMemberDto?> Handle(GetFamilyMemberByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        return await _tenantRepository.GetFamilyMemberDtoByIdAsync(request.TenantId, request.FamilyMemberId, companyId, cancellationToken);
    }
}
