using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;

namespace PropertyOS.Application.Financials.Queries.GetChequesByStatus;

public class GetChequesQueryHandler : IRequestHandler<GetChequesQuery, List<ChequeDetailDto>>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ITenantContext _tenantContext;

    public GetChequesQueryHandler(IRentPaymentRepository rentPaymentRepository, ITenantContext tenantContext)
    {
        _rentPaymentRepository = rentPaymentRepository;
        _tenantContext = tenantContext;
    }

    public Task<List<ChequeDetailDto>> Handle(GetChequesQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return _rentPaymentRepository.GetChequesAsync(request.Status, companyId, request.PageSize, cancellationToken);
    }
}
