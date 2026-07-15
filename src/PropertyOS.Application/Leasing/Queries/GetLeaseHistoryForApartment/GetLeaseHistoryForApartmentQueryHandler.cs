using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForApartment;

public class GetLeaseHistoryForApartmentQueryHandler : IRequestHandler<GetLeaseHistoryForApartmentQuery, List<LeaseContractDto>>
{
    private readonly ILeaseContractRepository _leaseContractRepository;

    public GetLeaseHistoryForApartmentQueryHandler(ILeaseContractRepository leaseContractRepository)
    {
        _leaseContractRepository = leaseContractRepository;
    }

    public async Task<List<LeaseContractDto>> Handle(GetLeaseHistoryForApartmentQuery request, CancellationToken cancellationToken)
    {
        return await _leaseContractRepository.GetHistoryByApartmentIdAsync(request.ApartmentId, cancellationToken);
    }
}
