using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Leasing;

namespace PropertyOS.Application.Financials.Queries.GetMyPayments;

/// <summary>
/// Handles <see cref="GetMyPaymentsQuery"/> by resolving the tenant from authenticated context,
/// then fetching rent payments scoped to that tenant and company.
///
/// Security model:
/// - companyId comes exclusively from ITenantContext (JWT-resolved, server-side)
/// - tenantId comes exclusively from a DB lookup keyed on ICurrentUserContext.UserId
/// - No client-supplied IDs are trusted (IDOR / BOLA prevention)
/// </summary>
public class GetMyPaymentsQueryHandler : IRequestHandler<GetMyPaymentsQuery, List<RentPaymentDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetMyPaymentsQueryHandler(
        ITenantRepository tenantRepository,
        IRentPaymentRepository rentPaymentRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _rentPaymentRepository = rentPaymentRepository ?? throw new ArgumentNullException(nameof(rentPaymentRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    }

    public async Task<List<RentPaymentDto>> Handle(GetMyPaymentsQuery request, CancellationToken cancellationToken)
    {
        // 1. Resolve company from server-side JWT context — never from client input.
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context (CompanyId) is required.");

        // 2. Resolve user from server-side JWT context — never from client input.
        var userId = _currentUserContext.UserId
            ?? throw new InvalidOperationException("Authenticated user context (UserId) is required.");

        // 3. Map authenticated UserId → Tenant aggregate within this company.
        //    Returns null when no Tenant record is linked to this user.
        var tenant = await _tenantRepository.GetByUserIdAsync(companyId, userId, cancellationToken);
        if (tenant == null)
            return new List<RentPaymentDto>();

        // 4. Retrieve rent payments. Both companyId and tenantId are server-derived,
        //    so cross-tenant and cross-user data leakage is structurally impossible.
        return await _rentPaymentRepository.GetPaymentsForTenantAsync(tenant.Id, companyId, cancellationToken);
    }
}
