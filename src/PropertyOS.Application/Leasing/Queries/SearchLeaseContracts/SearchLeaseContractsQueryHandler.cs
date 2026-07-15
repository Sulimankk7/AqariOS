using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Leasing.Queries.Common;

namespace PropertyOS.Application.Leasing.Queries.SearchLeaseContracts;

public class SearchLeaseContractsQueryHandler : IRequestHandler<SearchLeaseContractsQuery, List<LeaseContractDto>>
{
    private readonly ILeaseContractRepository _leaseContractRepository;

    public SearchLeaseContractsQueryHandler(ILeaseContractRepository leaseContractRepository)
    {
        _leaseContractRepository = leaseContractRepository;
    }

    public async Task<List<LeaseContractDto>> Handle(SearchLeaseContractsQuery request, CancellationToken cancellationToken)
    {
        return await _leaseContractRepository.SearchContractsAsync(request.SearchTerm, cancellationToken);
    }
}
