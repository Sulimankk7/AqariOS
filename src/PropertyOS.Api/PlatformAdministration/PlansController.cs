using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.PlatformAdministration.Requests;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.DTOs.Subscriptions;
using PropertyOS.Application.Subscriptions.DTOs;
using PropertyOS.Application.Subscriptions.Security;
using PropertyOS.Application.Subscriptions.UseCases;

namespace PropertyOS.Api.PlatformAdministration;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/platform/plans")]
[Authorize(Roles = PlatformRoles.SystemAdmin)]
[Tags("Platform Administration - Plans")]
[Produces("application/json", "application/problem+json")]
public sealed class PlansController : ControllerBase
{
    private readonly ISender _sender;
    public PlansController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlansRead)]
    public async Task<ActionResult<PlanPageDto>> GetPlans(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null, [FromQuery] string? search = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetPlatformPlansQuery(page, pageSize, isActive, search), cancellationToken));

    [HttpGet("{planId:guid}")]
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlansRead)]
    public async Task<ActionResult<SubscriptionPlanDto>> GetPlan(Guid planId, CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetPlatformPlanByIdQuery(planId), cancellationToken));

    [HttpPost]
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlansCreate)]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubscriptionPlanDto>> CreatePlan(
        [FromBody] CreatePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new CreatePlanCommand(
            request.Code, request.NameEn, request.NameAr, request.DescriptionEn, request.DescriptionAr,
            request.MonthlyPrice, request.YearlyPrice, request.Currency, request.MaxBuildings,
            request.MaxUsers, request.MaxStorageMb, request.FeatureFlags, request.SupportsTrial,
            request.TrialDurationDays, request.SortOrder, request.PricingModel,
            request.PaygMonthlyUnitPrice, request.PaygYearlyMonthlyEquivalentUnitPrice), cancellationToken);
        return CreatedAtAction(nameof(GetPlan), new { version = "1", planId = result.Id }, result);
    }

    [HttpPost("{planId:guid}/activate")]
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlansLifecycle)]
    public async Task<ActionResult<SubscriptionPlanDto>> Activate(Guid planId, CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new SetPlanActiveCommand(planId, true), cancellationToken));

    [HttpPost("{planId:guid}/deactivate")]
    [Authorize(Policy = SubscriptionsPermissions.PlatformPlansLifecycle)]
    public async Task<ActionResult<SubscriptionPlanDto>> Deactivate(Guid planId, CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new SetPlanActiveCommand(planId, false), cancellationToken));
}
