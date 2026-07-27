using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Companies.Security;
using PropertyOS.Application.DTOs.Subscriptions;
using PropertyOS.Application.Subscriptions;

namespace PropertyOS.Api.Controllers;

/// <summary>
/// API controller managing subscription plans and user/company active subscriptions.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/subscriptions")]
[Produces("application/json", "application/problem+json")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    /// <summary>
    /// Initializes a new instance of SubscriptionController.
    /// </summary>
    /// <param name="subscriptionService">The subscription application service.</param>
    public SubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
    }

    /// <summary>
    /// Gets all active subscription plans with optional pagination.
    /// </summary>
    /// <remarks>
    /// Public endpoint. Returns available subscription plans ordered by sort preference.
    /// </remarks>
    /// <param name="page">Page index (1-based, default 1).</param>
    /// <param name="pageSize">Page size (default 20, maximum 100).</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>A paginated list of active subscription plans.</returns>
    /// <response code="200">Returns the paginated list of active subscription plans.</response>
    /// <response code="400">If query parameters are invalid.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("plans")]
    [AllowAnonymous]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(PagedResultDto<SubscriptionPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResultDto<SubscriptionPlanDto>>> GetActivePlans(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1)
        {
            return Problem(
                title: "Invalid Pagination Parameters",
                detail: "Page and pageSize must be positive integers.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await _subscriptionService.GetActivePlansAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the current authenticated user's active subscription details.
    /// </summary>
    /// <remarks>
    /// Authorized endpoint. Returns the active subscription associated with the logged-in user's company.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>Active subscription details for authenticated user.</returns>
    /// <response code="200">Returns the user's current subscription.</response>
    /// <response code="401">If the request is unauthenticated or user claim is invalid.</response>
    /// <response code="404">If the user has no active subscription.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSubscriptionDto>> GetMySubscription(CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Problem(
                title: "Unauthorized",
                detail: "User identity claim could not be determined from token.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var subscription = await _subscriptionService.GetUserSubscriptionAsync(userId, cancellationToken);
        if (subscription == null)
        {
            return Problem(
                title: "Subscription Not Found",
                detail: "No active or recent subscription was found for the logged-in user.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(subscription);
    }

    /// <summary>
    /// Subscribes the authenticated user's company to a subscription plan.
    /// </summary>
    /// <remarks>
    /// Authorized endpoint. Enforces idempotency to prevent duplicate active subscriptions.
    /// </remarks>
    /// <param name="dto">The subscription creation request payload.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>The newly created subscription record.</returns>
    /// <response code="201">Subscription created successfully.</response>
    /// <response code="400">If the request body or DTO validation fails.</response>
    /// <response code="401">If the request is unauthenticated.</response>
    /// <response code="404">If the specified plan ID is not found or inactive.</response>
    /// <response code="409">If an active subscription already exists for the company.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("subscribe")]
    [Authorize(Policy = CompaniesPermissions.Manage)]
    [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSubscriptionDto>> Subscribe(
        [FromBody] CreateSubscriptionRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Problem(
                title: "Unauthorized",
                detail: "User identity claim could not be determined from token.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        try
        {
            var subscription = await _subscriptionService.SubscribeAsync(userId, dto, cancellationToken);
            return CreatedAtAction(nameof(GetMySubscription), new { version = "1" }, subscription);
        }
        catch (NotFoundException ex)
        {
            return Problem(
                title: "Resource Not Found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (ConflictException ex)
        {
            return Problem(
                title: "Subscription Conflict",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>
    /// Modifies, upgrades, or downgrades the user's active subscription plan.
    /// </summary>
    /// <remarks>
    /// Authorized endpoint. Uses optimistic concurrency protection.
    /// </remarks>
    /// <param name="dto">The plan change request payload.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>The updated subscription details.</returns>
    /// <response code="200">Subscription plan changed successfully.</response>
    /// <response code="400">If the request body or DTO validation fails.</response>
    /// <response code="401">If the request is unauthenticated.</response>
    /// <response code="404">If no active subscription or target plan exists.</response>
    /// <response code="409">If a concurrent edit collision occurs.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("change-plan")]
    [Authorize(Policy = CompaniesPermissions.Manage)]
    [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSubscriptionDto>> ChangePlan(
        [FromBody] ChangePlanRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Problem(
                title: "Unauthorized",
                detail: "User identity claim could not be determined from token.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        try
        {
            var subscription = await _subscriptionService.ChangePlanAsync(userId, dto, cancellationToken);
            return Ok(subscription);
        }
        catch (NotFoundException ex)
        {
            return Problem(
                title: "Resource Not Found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (ConflictException ex)
        {
            return Problem(
                title: "Concurrency Conflict",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>
    /// Cancels the authenticated user's active subscription safely.
    /// </summary>
    /// <remarks>
    /// Authorized endpoint. Sets status to Cancelled and disables auto-renewal.
    /// </remarks>
    /// <param name="request">Optional cancellation request payload containing cancellation reason.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>The updated subscription showing cancelled status.</returns>
    /// <response code="200">Subscription cancelled successfully.</response>
    /// <response code="401">If the request is unauthenticated.</response>
    /// <response code="404">If no active subscription is found to cancel.</response>
    /// <response code="409">If a concurrent state change occurs.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("cancel")]
    [Authorize(Policy = CompaniesPermissions.Manage)]
    [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSubscriptionDto>> Cancel(
        [FromBody] CancelSubscriptionRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Problem(
                title: "Unauthorized",
                detail: "User identity claim could not be determined from token.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        try
        {
            var subscription = await _subscriptionService.CancelSubscriptionAsync(userId, request?.Reason, cancellationToken);
            return Ok(subscription);
        }
        catch (NotFoundException ex)
        {
            return Problem(
                title: "Resource Not Found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (ConflictException ex)
        {
            return Problem(
                title: "Concurrency Conflict",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");

        return Guid.TryParse(claimValue, out userId);
    }
}
