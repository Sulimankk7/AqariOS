using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Maintenance.Queries.Common;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceAttachments;

public record GetMaintenanceAttachmentsQuery(Guid RequestId) : IRequest<List<MaintenanceAttachmentDto>>;

public class GetMaintenanceAttachmentsQueryHandler
    : IRequestHandler<GetMaintenanceAttachmentsQuery, List<MaintenanceAttachmentDto>>
{
    private readonly IMaintenanceQueries _queries;

    public GetMaintenanceAttachmentsQueryHandler(IMaintenanceQueries queries)
    {
        _queries = queries;
    }

    public Task<List<MaintenanceAttachmentDto>> Handle(
        GetMaintenanceAttachmentsQuery request,
        CancellationToken cancellationToken)
    {
        return _queries.GetAttachmentsAsync(request.RequestId, cancellationToken);
    }
}
