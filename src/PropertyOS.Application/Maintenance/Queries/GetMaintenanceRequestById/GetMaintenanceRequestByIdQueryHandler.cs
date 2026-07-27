using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Maintenance.Queries.Common;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceRequestById;

public class GetMaintenanceRequestByIdQueryHandler
    : IRequestHandler<GetMaintenanceRequestByIdQuery, MaintenanceRequestDetailDto?>
{
    private readonly IMaintenanceQueries _queries;
    private readonly ITenantContext _tenantContext;

    public GetMaintenanceRequestByIdQueryHandler(IMaintenanceQueries queries, ITenantContext tenantContext)
    {
        _queries = queries;
        _tenantContext = tenantContext;
    }

    public Task<MaintenanceRequestDetailDto?> Handle(
        GetMaintenanceRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        return _queries.GetDetailByIdAsync(request.Id, companyId, cancellationToken);
    }
}
