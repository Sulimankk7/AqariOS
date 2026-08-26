using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Queries.GetRentPaymentSettlementStatementPdf;
using PropertyOS.Application.Leasing;

namespace PropertyOS.Application.Financials.Queries.GetMyPaymentSettlementStatementPdf;

public record GetMyPaymentSettlementStatementPdfQuery(Guid RentPaymentId) : IRequest<SettlementStatementFileDto>;

public class GetMyPaymentSettlementStatementPdfQueryHandler : IRequestHandler<GetMyPaymentSettlementStatementPdfQuery, SettlementStatementFileDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISender _mediator;

    public GetMyPaymentSettlementStatementPdfQueryHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        ISender mediator)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public async Task<SettlementStatementFileDto> Handle(GetMyPaymentSettlementStatementPdfQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException();
        var tenant = await _tenantRepository.GetByUserIdAsync(companyId, userId, cancellationToken)
            ?? throw new NotFoundException("Tenant profile not found for authenticated user.");

        return await _mediator.Send(new GetRentPaymentSettlementStatementPdfQuery(request.RentPaymentId, tenant.Id), cancellationToken);
    }
}
