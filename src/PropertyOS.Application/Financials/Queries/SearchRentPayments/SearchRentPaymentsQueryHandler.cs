using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.SearchRentPayments;

public class SearchRentPaymentsQueryHandler : IRequestHandler<SearchRentPaymentsQuery, List<RentPaymentDto>>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ITenantContext _tenantContext;

    public SearchRentPaymentsQueryHandler(IRentPaymentRepository rentPaymentRepository, ITenantContext tenantContext)
    {
        _rentPaymentRepository = rentPaymentRepository;
        _tenantContext = tenantContext;
    }

    public Task<List<RentPaymentDto>> Handle(SearchRentPaymentsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return _rentPaymentRepository.SearchPaymentsAsync(request.SearchTerm, companyId, request.PageSize, cancellationToken);
    }
}
