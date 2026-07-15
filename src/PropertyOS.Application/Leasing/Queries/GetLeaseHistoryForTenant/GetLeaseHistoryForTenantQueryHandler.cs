using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForTenant;

public class GetLeaseHistoryForTenantQueryHandler : IRequestHandler<GetLeaseHistoryForTenantQuery, List<LeaseContractDto>>
{
    private readonly ILeaseContractRepository _leaseContractRepository;

    public GetLeaseHistoryForTenantQueryHandler(ILeaseContractRepository leaseContractRepository)
    {
        _leaseContractRepository = leaseContractRepository;
    }

    public async Task<List<LeaseContractDto>> Handle(GetLeaseHistoryForTenantQuery request, CancellationToken cancellationToken)
    {
        return await _leaseContractRepository.GetHistoryByTenantIdAsync(request.TenantId, cancellationToken);
    }
}
