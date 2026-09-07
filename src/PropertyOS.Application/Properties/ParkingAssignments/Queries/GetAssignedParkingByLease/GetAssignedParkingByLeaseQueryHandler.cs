using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Properties.ParkingAssignments.Queries.Common;

namespace PropertyOS.Application.Properties.ParkingAssignments.Queries.GetAssignedParkingByLease;

public class GetAssignedParkingByLeaseQueryHandler(
    IParkingAssignmentRepository assignments, ILeaseContractRepository leases, ITenantContext tenantContext)
    : IRequestHandler<GetAssignedParkingByLeaseQuery, List<ParkingAssignmentDto>>
{
    public async Task<List<ParkingAssignmentDto>> Handle(GetAssignedParkingByLeaseQuery request, CancellationToken cancellationToken)
    {
        var companyId = tenantContext.CompanyId ?? throw new UnauthorizedAccessException("Company context is required.");
        var lease = await leases.GetByIdAsync(request.LeaseContractId, cancellationToken);
        if (lease == null || lease.CompanyId != companyId || lease.DeletedAt != null)
            throw new NotFoundException("Lease contract was not found.");
        return await assignments.GetCurrentByLeaseAsync(lease.Id, companyId, cancellationToken);
    }
}
