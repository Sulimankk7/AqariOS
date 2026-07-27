using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentById;

public class GetRentPaymentByIdQueryHandler : IRequestHandler<GetRentPaymentByIdQuery, RentPaymentDetailDto?>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ITenantContext _tenantContext;

    public GetRentPaymentByIdQueryHandler(IRentPaymentRepository rentPaymentRepository, ITenantContext tenantContext)
    {
        _rentPaymentRepository = rentPaymentRepository;
        _tenantContext = tenantContext;
    }

    public Task<RentPaymentDetailDto?> Handle(GetRentPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return _rentPaymentRepository.GetDetailByIdAsync(request.Id, companyId, cancellationToken);
    }
}
