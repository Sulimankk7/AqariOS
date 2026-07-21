using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Marketplace.Queries.Common;

namespace PropertyOS.Application.Marketplace.Queries.GetPublicListings;

public class GetPublicListingsQueryHandler : IRequestHandler<GetPublicListingsQuery, List<PublicListingSummaryDto>>
{
    private readonly IMarketplaceQueries _queries;

    public GetPublicListingsQueryHandler(IMarketplaceQueries queries)
    {
        _queries = queries;
    }

    public Task<List<PublicListingSummaryDto>> Handle(GetPublicListingsQuery request, CancellationToken cancellationToken)
    {
        // Enforce server-side limit of maximum 50 rows per page
        var pageSize = Math.Min(request.PageSize > 0 ? request.PageSize : 10, 50);

        var filter = new PublicListingsFilterOptions(
            BuildingId: request.BuildingId,
            ApartmentId: request.ApartmentId,
            SearchText: request.SearchText,
            LastSeenIsFeatured: request.LastSeenIsFeatured,
            LastSeenPublishedDate: request.LastSeenPublishedDate,
            LastSeenId: request.LastSeenId,
            PageSize: pageSize);

        return _queries.GetPublicListingsAsync(filter, cancellationToken);
    }
}
