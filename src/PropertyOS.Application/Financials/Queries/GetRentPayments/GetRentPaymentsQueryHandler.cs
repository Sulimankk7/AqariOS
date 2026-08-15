using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetRentPayments;

public class GetRentPaymentsQueryHandler : IRequestHandler<GetRentPaymentsQuery, List<RentPaymentDto>>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ITenantContext _tenantContext;

    public GetRentPaymentsQueryHandler(IRentPaymentRepository rentPaymentRepository, ITenantContext tenantContext)
    {
        _rentPaymentRepository = rentPaymentRepository ?? throw new ArgumentNullException(nameof(rentPaymentRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public Task<List<RentPaymentDto>> Handle(GetRentPaymentsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var filter = new RentPaymentFilterOptions(
            BuildingId: request.BuildingId,
            Status: request.Status,
            DateFrom: request.DateFrom,
            DateTo: request.DateTo,
            SearchTerm: request.SearchTerm,
            LastSeenId: request.LastSeenId,
            LastSeenDueDate: request.LastSeenDueDate,
            PageSize: request.PageSize
        );

        return _rentPaymentRepository.GetPaymentsAsync(filter, companyId, cancellationToken);
    }
}
