using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Subscriptions.Requests;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.DTOs.Subscriptions;
using PropertyOS.Application.Subscriptions.DTOs;
using PropertyOS.Application.Subscriptions.Security;
using PropertyOS.Application.Subscriptions.UseCases;

namespace PropertyOS.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/subscriptions")]
[Authorize(Roles = CompanyRoles.CompanyAdmin)]
[Produces("application/json", "application/problem+json")]
public sealed class SubscriptionController : ControllerBase
{
    private readonly ISender _sender;

    public SubscriptionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("plans")]
    [Authorize(Policy = SubscriptionsPermissions.PlansView)]
    [ProducesResponseType(typeof(PlanPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PlanPageDto>> GetActivePlans(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetAvailablePlansQuery(page, pageSize), cancellationToken));

    [HttpGet("me")]
    [Authorize(Policy = SubscriptionsPermissions.OwnSubscriptionView)]
    [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSubscriptionDto>> GetMySubscription(CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetCurrentSubscriptionQuery(), cancellationToken));

    [HttpGet("plan-change-requests")]
    [Authorize(Policy = SubscriptionsPermissions.OwnPlanChangeRequestsRead)]
    [ProducesResponseType(typeof(PlanChangeRequestPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlanChangeRequestPageDto>> GetMyPlanChangeRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetMyPlanChangeRequestsQuery(page, pageSize), cancellationToken));

    [HttpPost("plan-change-requests")]
    [Authorize(Policy = SubscriptionsPermissions.OwnPlanChangeRequestsCreate)]
    [ProducesResponseType(typeof(PlanChangeRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlanChangeRequestDto>> CreatePlanChangeRequest(
        [FromBody] CreatePlanChangeRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new CreatePlanChangeRequestCommand(request.RequestedPlanId, request.RequestedBillingCycle),
            cancellationToken);
        return CreatedAtAction(nameof(GetMyPlanChangeRequests), new { version = "1" }, result);
    }

    [HttpPost("plan-change-requests/{requestId:guid}/cancel")]
    [Authorize(Policy = SubscriptionsPermissions.OwnPlanChangeRequestsCancel)]
    [ProducesResponseType(typeof(PlanChangeRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PlanChangeRequestDto>> CancelPlanChangeRequest(
        Guid requestId,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new CancelPlanChangeRequestCommand(requestId), cancellationToken));

    [Obsolete("Company administrators must use POST /plan-change-requests.")]
    [HttpPost("subscribe")]
    [Authorize(Policy = SubscriptionsPermissions.OwnPlanChangeRequestsCreate)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public ActionResult Subscribe() => LegacyMutationRetired();

    [Obsolete("Company administrators must use POST /plan-change-requests.")]
    [HttpPut("change-plan")]
    [Authorize(Policy = SubscriptionsPermissions.OwnPlanChangeRequestsCreate)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public ActionResult ChangePlan() => LegacyMutationRetired();

    [Obsolete("Subscription cancellation is not defined by the current Business Rules.")]
    [HttpPost("cancel")]
    [Authorize(Policy = SubscriptionsPermissions.OwnSubscriptionView)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public ActionResult Cancel() => Problem(
        statusCode: StatusCodes.Status410Gone,
        title: "Subscription cancellation unavailable",
        detail: "Subscription cancellation behavior is not defined by the current Business Rules.");

    private ActionResult LegacyMutationRetired() => Problem(
        statusCode: StatusCodes.Status410Gone,
        title: "Direct subscription mutation retired",
        detail: "Company administrators must submit a Plan Change Request for Platform Admin review.");
}
