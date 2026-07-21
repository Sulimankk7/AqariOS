using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Marketplace.Queries.Common;

namespace PropertyOS.Application.Marketplace.Queries.GetListingDetails;

public class GetListingDetailsQueryHandler : IRequestHandler<GetListingDetailsQuery, ListingDetailDto?>
{
    private readonly IMarketplaceQueries _queries;
    private readonly ITenantContext _tenantContext;

    public GetListingDetailsQueryHandler(
        IMarketplaceQueries queries,
        ITenantContext tenantContext)
    {
        _queries = queries;
        _tenantContext = tenantContext;
    }

    public Task<ListingDetailDto?> Handle(GetListingDetailsQuery request, CancellationToken cancellationToken)
    {
        // CompanyId is resolved from token if authenticated (staff view), or null if public view
        var companyId = _tenantContext.CompanyId;

        return _queries.GetListingDetailsAsync(request.Id, companyId, cancellationToken);
    }
}
