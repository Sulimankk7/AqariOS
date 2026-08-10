using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;

namespace PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications;

public class GetPendingPaymentVerificationsQueryHandler : IRequestHandler<GetPendingPaymentVerificationsQuery, KeysetPage<PaymentVerificationQueueItemDto>>
{
    private readonly IRentPaymentRepository _repository;
    private readonly ITenantContext _tenantContext;

    public GetPendingPaymentVerificationsQueryHandler(IRentPaymentRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<KeysetPage<PaymentVerificationQueueItemDto>> Handle(GetPendingPaymentVerificationsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var decodedCursor = KeysetCursor.Decode(request.Cursor);

        return await _repository.GetPendingVerificationsAsync(
            companyId,
            request.PageSize,
            decodedCursor?.SubmittedAt,
            decodedCursor?.Id,
            cancellationToken);
    }
}
