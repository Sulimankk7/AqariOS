using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.SearchRentPayments;

public class SearchRentPaymentsQueryHandler : IRequestHandler<SearchRentPaymentsQuery, List<RentPaymentDto>>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;

    public SearchRentPaymentsQueryHandler(IRentPaymentRepository rentPaymentRepository)
    {
        _rentPaymentRepository = rentPaymentRepository;
    }

    public Task<List<RentPaymentDto>> Handle(SearchRentPaymentsQuery request, CancellationToken cancellationToken)
    {
        return _rentPaymentRepository.SearchPaymentsAsync(request.SearchTerm, cancellationToken);
    }
}
