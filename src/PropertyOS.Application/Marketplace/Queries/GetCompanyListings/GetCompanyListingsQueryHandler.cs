using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Marketplace.Queries.Common;

namespace PropertyOS.Application.Marketplace.Queries.GetCompanyListings;

public class GetCompanyListingsQueryHandler : IRequestHandler<GetCompanyListingsQuery, List<CompanyListingSummaryDto>>
{
    private readonly IMarketplaceQueries _queries;
    private readonly ITenantContext _tenantContext;

    public GetCompanyListingsQueryHandler(
        IMarketplaceQueries queries,
        ITenantContext tenantContext)
    {
        _queries = queries;
        _tenantContext = tenantContext;
    }

    public Task<List<CompanyListingSummaryDto>> Handle(GetCompanyListingsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var pageSize = Math.Min(request.PageSize > 0 ? request.PageSize : 10, 50);

        var filter = new CompanyListingsFilterOptions(
            Status: request.Status,
            BuildingId: request.BuildingId,
            ApartmentId: request.ApartmentId,
            SearchText: request.SearchText,
            LastSeenPublishedDate: request.LastSeenPublishedDate,
            LastSeenId: request.LastSeenId,
            PageSize: pageSize);

        return _queries.GetCompanyListingsAsync(filter, companyId, cancellationToken);
    }
}
