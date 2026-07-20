using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentReceiptByRentPaymentId;

public class GetRentPaymentReceiptByRentPaymentIdQueryHandler
    : IRequestHandler<GetRentPaymentReceiptByRentPaymentIdQuery, RentPaymentReceiptDto?>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;

    public GetRentPaymentReceiptByRentPaymentIdQueryHandler(IRentPaymentRepository rentPaymentRepository)
    {
        _rentPaymentRepository = rentPaymentRepository;
    }

    public Task<RentPaymentReceiptDto?> Handle(
        GetRentPaymentReceiptByRentPaymentIdQuery request,
        CancellationToken cancellationToken)
    {
        return _rentPaymentRepository.GetReceiptByRentPaymentIdAsync(request.RentPaymentId, cancellationToken);
    }
}
