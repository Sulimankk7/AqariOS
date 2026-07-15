using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;

namespace PropertyOS.Application.Financials.Queries.GetChequesByStatus;

public class GetChequesByStatusQueryHandler : IRequestHandler<GetChequesByStatusQuery, List<ChequeDetailDto>>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;

    public GetChequesByStatusQueryHandler(IRentPaymentRepository rentPaymentRepository)
    {
        _rentPaymentRepository = rentPaymentRepository;
    }

    public Task<List<ChequeDetailDto>> Handle(GetChequesByStatusQuery request, CancellationToken cancellationToken)
    {
        return _rentPaymentRepository.GetChequesByStatusAsync(request.Status, cancellationToken);
    }
}
