using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;

namespace PropertyOS.Application.Financials.Queries.GetUpcomingCheques;

public class GetUpcomingChequesQueryHandler : IRequestHandler<GetUpcomingChequesQuery, List<ChequeDetailDto>>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;

    public GetUpcomingChequesQueryHandler(IRentPaymentRepository rentPaymentRepository)
    {
        _rentPaymentRepository = rentPaymentRepository;
    }

    public Task<List<ChequeDetailDto>> Handle(GetUpcomingChequesQuery request, CancellationToken cancellationToken)
    {
        return _rentPaymentRepository.GetUpcomingChequesAsync(request.DaysAhead, cancellationToken);
    }
}
