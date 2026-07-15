using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.GetExpiringLeases;

public class GetExpiringLeasesQueryHandler : IRequestHandler<GetExpiringLeasesQuery, List<LeaseContractDto>>
{
    private readonly ILeaseContractRepository _leaseContractRepository;

    public GetExpiringLeasesQueryHandler(ILeaseContractRepository leaseContractRepository)
    {
        _leaseContractRepository = leaseContractRepository;
    }

    public async Task<List<LeaseContractDto>> Handle(GetExpiringLeasesQuery request, CancellationToken cancellationToken)
    {
        return await _leaseContractRepository.GetExpiringLeasesAsync(request.DaysAhead, cancellationToken);
    }
}
