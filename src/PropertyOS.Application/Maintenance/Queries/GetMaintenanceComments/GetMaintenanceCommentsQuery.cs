using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Maintenance.Queries.Common;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceComments;

public record GetMaintenanceCommentsQuery(Guid RequestId) : IRequest<List<MaintenanceCommentDto>>;

public class GetMaintenanceCommentsQueryHandler
    : IRequestHandler<GetMaintenanceCommentsQuery, List<MaintenanceCommentDto>>
{
    private readonly IMaintenanceQueries _queries;

    public GetMaintenanceCommentsQueryHandler(IMaintenanceQueries queries)
    {
        _queries = queries;
    }

    public Task<List<MaintenanceCommentDto>> Handle(
        GetMaintenanceCommentsQuery request,
        CancellationToken cancellationToken)
    {
        return _queries.GetCommentsAsync(request.RequestId, cancellationToken);
    }
}
