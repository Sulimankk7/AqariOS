using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.PlatformAdministration.Requests;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.Subscriptions.DTOs;
using PropertyOS.Application.Subscriptions.Security;
using PropertyOS.Application.Subscriptions.UseCases;
using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Api.PlatformAdministration;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/platform/plan-change-requests")]
[Authorize(Roles = PlatformRoles.SystemAdmin)]
[Tags("Platform Administration - Plan Change Requests")]
[Produces("application/json", "application/problem+json")]
public sealed class PlanChangeRequestsController : ControllerBase
{
    private readonly ISender _sender;
    public PlanChangeRequestsController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlanChangeRequestsRead)]
    public async Task<ActionResult<PlanChangeRequestPageDto>> GetRequests(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] PlanChangeRequestStatus? status = null,
        [FromQuery] Guid? companyId = null, [FromQuery] string? search = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetPlatformPlanChangeRequestsQuery(
            page, pageSize, status, companyId, search), cancellationToken));

    [HttpGet("{requestId:guid}")]
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlanChangeRequestsRead)]
    public async Task<ActionResult<PlanChangeRequestDto>> GetRequest(
        Guid requestId, CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetPlatformPlanChangeRequestByIdQuery(requestId), cancellationToken));

    [HttpPost("{requestId:guid}/approve")]
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlanChangeRequestsReview)]
    [ProducesResponseType(typeof(PlanChangeRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PlanChangeRequestDto>> Approve(
        Guid requestId, [FromBody] ApprovePlanChangeRequestRequest? request,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new ApprovePlanChangeRequestCommand(requestId, request?.DecisionNote), cancellationToken));

    [HttpPost("{requestId:guid}/reject")]
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlanChangeRequestsReview)]
    [ProducesResponseType(typeof(PlanChangeRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PlanChangeRequestDto>> Reject(
        Guid requestId, [FromBody] RejectPlanChangeRequestRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new RejectPlanChangeRequestCommand(
            requestId, request.RejectionReason, request.DecisionNote), cancellationToken));
}
