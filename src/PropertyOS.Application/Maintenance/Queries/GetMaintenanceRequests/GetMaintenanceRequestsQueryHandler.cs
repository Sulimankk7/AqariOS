using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Maintenance.Queries.Common;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceRequests;

public class GetMaintenanceRequestsQueryHandler
    : IRequestHandler<GetMaintenanceRequestsQuery, List<MaintenanceRequestSummaryDto>>
{
    private readonly IMaintenanceQueries _queries;

    public GetMaintenanceRequestsQueryHandler(IMaintenanceQueries queries)
    {
        _queries = queries;
    }

    public Task<List<MaintenanceRequestSummaryDto>> Handle(
        GetMaintenanceRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new MaintenanceRequestFilterOptions(
            BuildingId: request.BuildingId,
            ApartmentId: request.ApartmentId,
            TenantId: request.TenantId,
            Status: request.Status,
            Priority: request.Priority,
            Category: request.Category,
            DateFrom: request.DateFrom,
            DateTo: request.DateTo,
            SearchText: request.SearchText,
            LastSeenId: request.LastSeenId,
            LastSeenRequestDate: request.LastSeenRequestDate,
            PageSize: request.PageSize);

        return _queries.GetRequestsAsync(filter, cancellationToken);
    }
}
