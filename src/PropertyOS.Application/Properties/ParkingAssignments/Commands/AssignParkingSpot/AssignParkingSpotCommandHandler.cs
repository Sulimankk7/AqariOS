using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties.ParkingAssignments.Commands.AssignParkingSpot;

public class AssignParkingSpotCommandHandler(
    IParkingAssignmentRepository assignments,
    IParkingSpotRepository spots,
    ILeaseContractRepository leases,
    ITenantRepository tenants,
    IBuildingRepository buildings,
    IApartmentRepository apartments,
    ITenantContext tenantContext,
    ICurrentUserContext currentUser,
    IBusinessClock clock) : IRequestHandler<AssignParkingSpotCommand, Guid>
{
    public async Task<Guid> Handle(AssignParkingSpotCommand request, CancellationToken cancellationToken)
    {
        var companyId = tenantContext.CompanyId ?? throw new UnauthorizedAccessException("Company context is required.");
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Authentication is required.");

        // Fixed lock order: lease, then spot. Locks live until TransactionBehavior commits.
        var lease = await leases.GetByIdForUpdateAsync(request.LeaseContractId, companyId, cancellationToken);
        if (lease == null || lease.CompanyId != companyId || lease.DeletedAt != null)
            throw new NotFoundException("Lease contract was not found.");
        var spot = await spots.GetByIdForUpdateAsync(request.ParkingSpotId, companyId, cancellationToken);
        if (spot == null || spot.CompanyId != companyId || spot.DeletedAt != null)
            throw new NotFoundException("Parking spot was not found.");
        if (!spot.IsActive)
            throw new BusinessRuleException("Parking spot is inactive.", "PARKING_SPOT_INACTIVE");

        var now = clock.UtcNow;
        var today = clock.GetJordanBusinessDate(now);
        // Leasing's actual state and term boundary: expiration is due on EndDate.
        if (lease.Status != ContractStatus.Active || lease.StartDate > today || lease.EndDate <= today)
            throw new BusinessRuleException("Lease contract is not active within its term.", "LEASE_NOT_ACTIVE");
        if (spot.BuildingId != lease.BuildingId)
            throw new BusinessRuleException("Parking spot and lease must belong to the same building.", "PARKING_BUILDING_MISMATCH");

        var tenant = await tenants.GetByIdAsync(lease.TenantId, cancellationToken);
        var building = await buildings.GetByIdAsync(lease.BuildingId, cancellationToken);
        var apartment = await apartments.GetByIdAsync(lease.ApartmentId, companyId, cancellationToken);
        if (tenant == null || tenant.CompanyId != companyId || tenant.DeletedAt != null
            || building == null || building.CompanyId != companyId || building.DeletedAt != null || !building.IsActive
            || apartment == null || apartment.CompanyId != companyId || apartment.DeletedAt != null
            || apartment.BuildingId != lease.BuildingId)
            throw new NotFoundException("Lease contract was not found in an accessible building.");

        if (await assignments.HasActiveAssignmentAsync(spot.Id, companyId, cancellationToken))
            throw new ConflictException("Parking spot already has an active assignment.", "PARKING_ALREADY_ASSIGNED");

        var assignment = ParkingAssignment.Create(companyId, spot.Id, lease.Id, today, now, userId);
        await assignments.AddAsync(assignment, cancellationToken);
        // The partial unique index remains the final arbiter, including non-API writers.
        return assignment.Id;
    }
}
