using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Marketplace.Queries.Common;

namespace PropertyOS.Application.Marketplace.Queries.GetViewingRequests;

public class GetViewingRequestsQueryHandler : IRequestHandler<GetViewingRequestsQuery, List<ViewingRequestSummaryDto>>
{
    private readonly IMarketplaceQueries _queries;
    private readonly ITenantContext _tenantContext;

    public GetViewingRequestsQueryHandler(
        IMarketplaceQueries queries,
        ITenantContext tenantContext)
    {
        _queries = queries;
        _tenantContext = tenantContext;
    }

    public Task<List<ViewingRequestSummaryDto>> Handle(GetViewingRequestsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var pageSize = Math.Min(request.PageSize > 0 ? request.PageSize : 10, 50);

        var filter = new ViewingRequestsFilterOptions(
            ListingId: request.ListingId,
            Status: request.Status,
            LastSeenSubmittedAt: request.LastSeenSubmittedAt,
            LastSeenId: request.LastSeenId,
            PageSize: pageSize);

        return _queries.GetViewingRequestsAsync(filter, companyId, cancellationToken);
    }
}
