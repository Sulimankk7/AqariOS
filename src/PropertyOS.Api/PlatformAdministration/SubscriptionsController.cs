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
[Route("api/v{version:apiVersion}/platform/subscriptions")]
[Authorize(Roles = PlatformRoles.SystemAdmin)]
[Tags("Platform Administration - Subscriptions")]
[Produces("application/json", "application/problem+json")]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly ISender _sender;
    public SubscriptionsController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize(Policy = SubscriptionsPermissions.PlatformSubscriptionsRead)]
    public async Task<ActionResult<SubscriptionPageDto>> GetSubscriptions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] SubscriptionStatusEnum? status = null,
        [FromQuery] BillingCycleEnum? billingCycle = null, [FromQuery] Guid? planId = null,
        [FromQuery] string sortBy = "createdAt", [FromQuery] bool descending = true,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetPlatformSubscriptionsQuery(
            page, pageSize, search, status, billingCycle, planId, sortBy, descending), cancellationToken));

    [HttpGet("{subscriptionId:guid}")]
    [Authorize(Policy = SubscriptionsPermissions.PlatformSubscriptionsRead)]
    public async Task<ActionResult<SubscriptionAdministrationDto>> GetSubscription(
        Guid subscriptionId, CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetPlatformSubscriptionByIdQuery(subscriptionId), cancellationToken));

    [HttpPost]
    [Authorize(Policy = SubscriptionsPermissions.PlatformSubscriptionsManage)]
    [ProducesResponseType(typeof(SubscriptionAdministrationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubscriptionAdministrationDto>> CreateSubscription(
        [FromBody] CreateCompanySubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new CreatePlatformSubscriptionCommand(
            request.CompanyId, request.PlanId, request.BillingCycle,
            request.StartDate, request.EndDate, request.TrialEndDate), cancellationToken);
        return CreatedAtAction(nameof(GetSubscription), new { version = "1", subscriptionId = result.Id }, result);
    }
}
