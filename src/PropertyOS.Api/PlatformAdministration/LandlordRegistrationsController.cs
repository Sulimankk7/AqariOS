using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.PlatformAdministration.Requests;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.PlatformAdministration.Commands.ApproveLandlordRegistration;
using PropertyOS.Application.PlatformAdministration.Commands.RejectLandlordRegistration;
using PropertyOS.Application.PlatformAdministration.DTOs;
using PropertyOS.Application.PlatformAdministration.Queries.GetLandlordRegistrationById;
using PropertyOS.Application.PlatformAdministration.Queries.GetPendingLandlordRegistrations;

namespace PropertyOS.Api.PlatformAdministration;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/platform/landlord-registrations")]
[Authorize(Roles = PlatformRoles.SystemAdmin)]
[Tags("Platform Administration")]
[Produces("application/json", "application/problem+json")]
public sealed class LandlordRegistrationsController : ControllerBase
{
    private readonly ISender _sender;

    public LandlordRegistrationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("pending")]
    [Authorize(Policy = PlatformPermissions.LandlordRegistrationsRead)]
    [ProducesResponseType(typeof(LandlordRegistrationPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LandlordRegistrationPageDto>> GetPending(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(
            new GetPendingLandlordRegistrationsQuery(page, pageSize),
            cancellationToken));
    }

    [HttpGet("{registrationId:guid}")]
    [Authorize(Policy = PlatformPermissions.LandlordRegistrationsRead)]
    [ProducesResponseType(typeof(LandlordRegistrationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LandlordRegistrationDetailDto>> GetById(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(
            new GetLandlordRegistrationByIdQuery(registrationId),
            cancellationToken));
    }

    [HttpPost("{registrationId:guid}/approve")]
    [Authorize(Policy = PlatformPermissions.LandlordRegistrationsApprove)]
    [ProducesResponseType(typeof(LandlordRegistrationReviewResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LandlordRegistrationReviewResultDto>> Approve(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(
            new ApproveLandlordRegistrationCommand(registrationId),
            cancellationToken));
    }

    [HttpPost("{registrationId:guid}/reject")]
    [Authorize(Policy = PlatformPermissions.LandlordRegistrationsReject)]
    [ProducesResponseType(typeof(LandlordRegistrationReviewResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LandlordRegistrationReviewResultDto>> Reject(
        Guid registrationId,
        [FromBody] RejectLandlordRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(
            new RejectLandlordRegistrationCommand(registrationId, request.Reason),
            cancellationToken));
    }
}
