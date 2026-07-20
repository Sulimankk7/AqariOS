using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Maintenance.Queries.Common;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceRequestById;

public class GetMaintenanceRequestByIdQueryHandler
    : IRequestHandler<GetMaintenanceRequestByIdQuery, MaintenanceRequestDetailDto?>
{
    private readonly IMaintenanceQueries _queries;

    public GetMaintenanceRequestByIdQueryHandler(IMaintenanceQueries queries)
    {
        _queries = queries;
    }

    public Task<MaintenanceRequestDetailDto?> Handle(
        GetMaintenanceRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _queries.GetDetailByIdAsync(request.Id, cancellationToken);
    }
}
