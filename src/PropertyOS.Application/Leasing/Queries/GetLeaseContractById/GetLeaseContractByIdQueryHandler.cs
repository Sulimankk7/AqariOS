using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseContractById;

public class GetLeaseContractByIdQueryHandler : IRequestHandler<GetLeaseContractByIdQuery, LeaseContractDetailDto?>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ITenantContext _tenantContext;

    public GetLeaseContractByIdQueryHandler(ILeaseContractRepository leaseContractRepository, ITenantContext tenantContext)
    {
        _leaseContractRepository = leaseContractRepository;
        _tenantContext = tenantContext;
    }

    public async Task<LeaseContractDetailDto?> Handle(GetLeaseContractByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return await _leaseContractRepository.GetDetailByIdAsync(request.Id, companyId, cancellationToken);
    }
}
