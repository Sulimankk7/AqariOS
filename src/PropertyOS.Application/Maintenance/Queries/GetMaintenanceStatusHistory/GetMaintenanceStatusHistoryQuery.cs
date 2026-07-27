using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Maintenance.Queries.Common;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceStatusHistory;

public record GetMaintenanceStatusHistoryQuery(Guid RequestId) : IRequest<List<MaintenanceStatusHistoryDto>>;

public class GetMaintenanceStatusHistoryQueryHandler
    : IRequestHandler<GetMaintenanceStatusHistoryQuery, List<MaintenanceStatusHistoryDto>>
{
    private readonly IMaintenanceQueries _queries;
    private readonly ITenantContext _tenantContext;

    public GetMaintenanceStatusHistoryQueryHandler(IMaintenanceQueries queries, ITenantContext tenantContext)
    {
        _queries = queries;
        _tenantContext = tenantContext;
    }

    public Task<List<MaintenanceStatusHistoryDto>> Handle(
        GetMaintenanceStatusHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return _queries.GetStatusHistoryAsync(request.RequestId, companyId, cancellationToken);
    }
}
