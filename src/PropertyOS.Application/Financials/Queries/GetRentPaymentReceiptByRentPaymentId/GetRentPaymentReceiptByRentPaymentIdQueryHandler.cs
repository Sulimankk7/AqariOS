using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentReceiptByRentPaymentId;

public class GetRentPaymentReceiptByRentPaymentIdQueryHandler
    : IRequestHandler<GetRentPaymentReceiptByRentPaymentIdQuery, RentPaymentReceiptDto?>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ITenantContext _tenantContext;

    public GetRentPaymentReceiptByRentPaymentIdQueryHandler(IRentPaymentRepository rentPaymentRepository, ITenantContext tenantContext)
    {
        _rentPaymentRepository = rentPaymentRepository;
        _tenantContext = tenantContext;
    }

    public Task<RentPaymentReceiptDto?> Handle(
        GetRentPaymentReceiptByRentPaymentIdQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return _rentPaymentRepository.GetReceiptByRentPaymentIdAsync(request.RentPaymentId, companyId, cancellationToken);
    }
}
