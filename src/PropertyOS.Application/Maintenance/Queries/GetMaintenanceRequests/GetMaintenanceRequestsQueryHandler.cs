using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Maintenance.Queries.Common;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceRequests;

public class GetMaintenanceRequestsQueryHandler
    : IRequestHandler<GetMaintenanceRequestsQuery, List<MaintenanceRequestSummaryDto>>
{
    private readonly IMaintenanceQueries _queries;
    private readonly ITenantContext _tenantContext;

    public GetMaintenanceRequestsQueryHandler(IMaintenanceQueries queries, ITenantContext tenantContext)
    {
        _queries = queries;
        _tenantContext = tenantContext;
    }

    public Task<List<MaintenanceRequestSummaryDto>> Handle(
        GetMaintenanceRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

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

        return _queries.GetRequestsAsync(filter, companyId, cancellationToken);
    }
}
