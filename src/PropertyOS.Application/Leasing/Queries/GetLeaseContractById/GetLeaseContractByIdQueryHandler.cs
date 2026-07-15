using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseContractById;

public class GetLeaseContractByIdQueryHandler : IRequestHandler<GetLeaseContractByIdQuery, LeaseContractDetailDto?>
{
    private readonly ILeaseContractRepository _leaseContractRepository;

    public GetLeaseContractByIdQueryHandler(ILeaseContractRepository leaseContractRepository)
    {
        _leaseContractRepository = leaseContractRepository;
    }

    public async Task<LeaseContractDetailDto?> Handle(GetLeaseContractByIdQuery request, CancellationToken cancellationToken)
    {
        return await _leaseContractRepository.GetDetailByIdAsync(request.Id, cancellationToken);
    }
}
