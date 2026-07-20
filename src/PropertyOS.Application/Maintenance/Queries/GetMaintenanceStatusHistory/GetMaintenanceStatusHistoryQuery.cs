using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Maintenance.Queries.Common;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceStatusHistory;

public record GetMaintenanceStatusHistoryQuery(Guid RequestId) : IRequest<List<MaintenanceStatusHistoryDto>>;

public class GetMaintenanceStatusHistoryQueryHandler
    : IRequestHandler<GetMaintenanceStatusHistoryQuery, List<MaintenanceStatusHistoryDto>>
{
    private readonly IMaintenanceQueries _queries;

    public GetMaintenanceStatusHistoryQueryHandler(IMaintenanceQueries queries)
    {
        _queries = queries;
    }

    public Task<List<MaintenanceStatusHistoryDto>> Handle(
        GetMaintenanceStatusHistoryQuery request,
        CancellationToken cancellationToken)
    {
        return _queries.GetStatusHistoryAsync(request.RequestId, cancellationToken);
    }
}
