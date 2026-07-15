using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetOutstandingRentPayments;

public class GetOutstandingRentPaymentsQueryHandler : IRequestHandler<GetOutstandingRentPaymentsQuery, List<RentPaymentDto>>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;

    public GetOutstandingRentPaymentsQueryHandler(IRentPaymentRepository rentPaymentRepository)
    {
        _rentPaymentRepository = rentPaymentRepository;
    }

    public Task<List<RentPaymentDto>> Handle(GetOutstandingRentPaymentsQuery request, CancellationToken cancellationToken)
    {
        return _rentPaymentRepository.GetOutstandingPaymentsAsync(cancellationToken);
    }
}
