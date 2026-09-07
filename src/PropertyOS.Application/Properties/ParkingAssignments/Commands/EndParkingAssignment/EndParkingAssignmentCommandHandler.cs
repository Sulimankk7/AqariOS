using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.ParkingAssignments.Commands.EndParkingAssignment;

public class EndParkingAssignmentCommandHandler(
    IParkingAssignmentRepository assignments, ITenantContext tenantContext, ICurrentUserContext currentUser, IBusinessClock clock)
    : IRequestHandler<EndParkingAssignmentCommand, Unit>
{
    public async Task<Unit> Handle(EndParkingAssignmentCommand request, CancellationToken cancellationToken)
    {
        var companyId = tenantContext.CompanyId ?? throw new UnauthorizedAccessException("Company context is required.");
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Authentication is required.");
        var assignment = await assignments.GetByIdForUpdateAsync(request.AssignmentId, companyId, cancellationToken);
        if (assignment == null || assignment.CompanyId != companyId || assignment.DeletedAt != null)
            throw new NotFoundException("Parking assignment was not found.");
        if (assignment.Status == ParkingAssignmentStatus.Ended) return Unit.Value;

        var now = clock.UtcNow;
        var today = clock.GetJordanBusinessDate(now);
        if (today < assignment.AssignedFrom)
            throw new BusinessRuleException("Assignment has not started yet.", "ASSIGNMENT_NOT_STARTED");
        assignment.EndAssignment(today, now, userId);
        return Unit.Value;
    }
}
