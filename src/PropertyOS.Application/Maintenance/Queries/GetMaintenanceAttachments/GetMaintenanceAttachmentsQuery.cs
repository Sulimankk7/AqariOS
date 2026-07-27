using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Maintenance.Queries.Common;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceAttachments;

public record GetMaintenanceAttachmentsQuery(Guid RequestId) : IRequest<List<MaintenanceAttachmentDto>>;

public class GetMaintenanceAttachmentsQueryHandler
    : IRequestHandler<GetMaintenanceAttachmentsQuery, List<MaintenanceAttachmentDto>>
{
    private readonly IMaintenanceQueries _queries;
    private readonly ITenantContext _tenantContext;

    public GetMaintenanceAttachmentsQueryHandler(IMaintenanceQueries queries, ITenantContext tenantContext)
    {
        _queries = queries;
        _tenantContext = tenantContext;
    }

    public Task<List<MaintenanceAttachmentDto>> Handle(
        GetMaintenanceAttachmentsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return _queries.GetAttachmentsAsync(request.RequestId, companyId, cancellationToken);
    }
}
