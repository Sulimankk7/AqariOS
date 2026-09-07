using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties.ParkingAssignments.Queries.Common;

namespace PropertyOS.Application.Properties.ParkingAssignments.Queries.GetCurrentAssignmentBySpot;

public class GetCurrentAssignmentBySpotQueryHandler(
    IParkingAssignmentRepository assignments, IParkingSpotRepository spots, ITenantContext tenantContext)
    : IRequestHandler<GetCurrentAssignmentBySpotQuery, ParkingAssignmentDto?>
{
    public async Task<ParkingAssignmentDto?> Handle(GetCurrentAssignmentBySpotQuery request, CancellationToken cancellationToken)
    {
        var companyId = tenantContext.CompanyId ?? throw new UnauthorizedAccessException("Company context is required.");
        var spot = await spots.GetByIdAsync(request.ParkingSpotId, cancellationToken);
        if (spot == null || spot.CompanyId != companyId || spot.DeletedAt != null)
            throw new NotFoundException("Parking spot was not found.");
        return await assignments.GetCurrentBySpotAsync(spot.Id, companyId, cancellationToken);
    }
}
