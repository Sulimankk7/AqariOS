using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Maintenance.Queries.Common;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceComments;

public record GetMaintenanceCommentsQuery(Guid RequestId) : IRequest<List<MaintenanceCommentDto>>;

public class GetMaintenanceCommentsQueryHandler
    : IRequestHandler<GetMaintenanceCommentsQuery, List<MaintenanceCommentDto>>
{
    private readonly IMaintenanceQueries _queries;
    private readonly ITenantContext _tenantContext;

    public GetMaintenanceCommentsQueryHandler(IMaintenanceQueries queries, ITenantContext tenantContext)
    {
        _queries = queries;
        _tenantContext = tenantContext;
    }

    public Task<List<MaintenanceCommentDto>> Handle(
        GetMaintenanceCommentsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return _queries.GetCommentsAsync(request.RequestId, companyId, cancellationToken);
    }
}
