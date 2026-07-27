using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentReceipts;

public class GetRentPaymentReceiptsQueryHandler : IRequestHandler<GetRentPaymentReceiptsQuery, List<RentPaymentReceiptDto>>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ITenantContext _tenantContext;

    public GetRentPaymentReceiptsQueryHandler(IRentPaymentRepository rentPaymentRepository, ITenantContext tenantContext)
    {
        _rentPaymentRepository = rentPaymentRepository;
        _tenantContext = tenantContext;
    }

    public Task<List<RentPaymentReceiptDto>> Handle(GetRentPaymentReceiptsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var filter = new RentPaymentReceiptFilterOptions(
            LeaseContractId: request.LeaseContractId,
            TenantId: request.TenantId,
            DateFrom: request.DateFrom,
            DateTo: request.DateTo,
            LastSeenId: request.LastSeenId,
            LastSeenIssueDate: request.LastSeenIssueDate,
            PageSize: request.PageSize
        );

        return _rentPaymentRepository.GetReceiptsAsync(filter, companyId, cancellationToken);
    }
}
