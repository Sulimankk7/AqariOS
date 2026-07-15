using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentById;

public class GetRentPaymentByIdQueryHandler : IRequestHandler<GetRentPaymentByIdQuery, RentPaymentDetailDto?>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;

    public GetRentPaymentByIdQueryHandler(IRentPaymentRepository rentPaymentRepository)
    {
        _rentPaymentRepository = rentPaymentRepository;
    }

    public Task<RentPaymentDetailDto?> Handle(GetRentPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        return _rentPaymentRepository.GetDetailByIdAsync(request.Id, cancellationToken);
    }
}
