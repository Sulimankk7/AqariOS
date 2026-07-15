using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentsForTenant;

public class GetRentPaymentsForTenantQueryHandler : IRequestHandler<GetRentPaymentsForTenantQuery, List<RentPaymentDto>>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;

    public GetRentPaymentsForTenantQueryHandler(IRentPaymentRepository rentPaymentRepository)
    {
        _rentPaymentRepository = rentPaymentRepository;
    }

    public Task<List<RentPaymentDto>> Handle(GetRentPaymentsForTenantQuery request, CancellationToken cancellationToken)
    {
        return _rentPaymentRepository.GetPaymentsForTenantAsync(request.TenantId, cancellationToken);
    }
}
