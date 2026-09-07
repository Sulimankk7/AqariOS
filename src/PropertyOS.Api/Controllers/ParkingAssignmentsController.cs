using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Models.Properties;
using PropertyOS.Application.Properties.ParkingAssignments.Commands.AssignParkingSpot;
using PropertyOS.Application.Properties.ParkingAssignments.Commands.EndParkingAssignment;
using PropertyOS.Application.Properties.ParkingAssignments.Queries.Common;
using PropertyOS.Application.Properties.ParkingAssignments.Queries.GetAssignedParkingByLease;
using PropertyOS.Application.Properties.ParkingAssignments.Queries.GetCurrentAssignmentBySpot;
using PropertyOS.Application.Properties.Security;

namespace PropertyOS.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class ParkingAssignmentsController(ISender mediator) : ControllerBase
{
    [HttpPost("api/v{version:apiVersion}/parking-spots/{parkingSpotId:guid}/assignment")]
    [Authorize(Policy = PropertyPermissions.Update)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> Assign(Guid parkingSpotId, AssignParkingSpotRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new AssignParkingSpotCommand(parkingSpotId, request.LeaseContractId), cancellationToken);
        return CreatedAtAction(nameof(GetBySpot), new { parkingSpotId, version = "1" }, new { assignmentId = id });
    }

    [HttpGet("api/v{version:apiVersion}/parking-spots/{parkingSpotId:guid}/assignment")]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(ParkingAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetBySpot(Guid parkingSpotId, CancellationToken cancellationToken)
    {
        var assignment = await mediator.Send(new GetCurrentAssignmentBySpotQuery(parkingSpotId), cancellationToken);
        return assignment == null ? NoContent() : Ok(assignment);
    }

    [HttpGet("api/v{version:apiVersion}/leasing/contracts/{leaseContractId:guid}/parking")]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(List<ParkingAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByLease(Guid leaseContractId, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetAssignedParkingByLeaseQuery(leaseContractId), cancellationToken));

    // Ending changes allocation state, not the parking asset or its history.
    [HttpPost("api/v{version:apiVersion}/parking-assignments/{assignmentId:guid}/end")]
    [Authorize(Policy = PropertyPermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> End(Guid assignmentId, CancellationToken cancellationToken)
    {
        await mediator.Send(new EndParkingAssignmentCommand(assignmentId), cancellationToken);
        return NoContent();
    }
}
