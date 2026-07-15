using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentsForLease;

public class GetRentPaymentsForLeaseQueryHandler : IRequestHandler<GetRentPaymentsForLeaseQuery, List<RentPaymentDto>>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;

    public GetRentPaymentsForLeaseQueryHandler(IRentPaymentRepository rentPaymentRepository)
    {
        _rentPaymentRepository = rentPaymentRepository;
    }

    public Task<List<RentPaymentDto>> Handle(GetRentPaymentsForLeaseQuery request, CancellationToken cancellationToken)
    {
        return _rentPaymentRepository.GetPaymentsForLeaseAsync(request.LeaseContractId, cancellationToken);
    }
}
