using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.DTOs.Subscriptions;

namespace PropertyOS.Application.Subscriptions;

/// <summary>
/// Service interface for managing subscription plans and user/company subscriptions.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Retrieves active subscription plans with pagination support.
    /// </summary>
    /// <param name="page">Page index (1-based, default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 100).</param>
    /// <param name="cancellationToken">Cancellation token to cancel execution.</param>
    /// <returns>A paginated result containing active subscription plans.</returns>
    Task<PagedResultDto<SubscriptionPlanDto>> GetActivePlansAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current active or recent subscription for the authenticated user's company.
    /// </summary>
    /// <param name="userId">The authenticated user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel execution.</param>
    /// <returns>The subscription details, or null if no subscription exists.</returns>
    Task<UserSubscriptionDto?> GetUserSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy direct tenant subscription mutation. Retired by the plans/subscriptions business rules.
    /// </summary>
    /// <param name="userId">The authenticated user's unique identifier.</param>
    /// <param name="dto">The subscription creation request parameters.</param>
    /// <param name="cancellationToken">Cancellation token to cancel execution.</param>
    /// <returns>The newly created subscription details.</returns>
    [Obsolete("Tenant self-service subscription creation is retired. Use platform subscription administration instead.")]
    Task<UserSubscriptionDto> SubscribeAsync(
        Guid userId,
        CreateSubscriptionRequestDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy direct tenant plan mutation. Retired by the plans/subscriptions business rules.
    /// </summary>
    /// <param name="userId">The authenticated user's unique identifier.</param>
    /// <param name="dto">The plan change request parameters.</param>
    /// <param name="cancellationToken">Cancellation token to cancel execution.</param>
    /// <returns>The updated subscription details.</returns>
    [Obsolete("Direct tenant plan changes are retired. Use the plan change request workflow instead.")]
    Task<UserSubscriptionDto> ChangePlanAsync(
        Guid userId,
        ChangePlanRequestDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy direct tenant cancellation. Retired by the plans/subscriptions business rules.
    /// </summary>
    /// <param name="userId">The authenticated user's unique identifier.</param>
    /// <param name="reason">Optional cancellation reason.</param>
    /// <param name="cancellationToken">Cancellation token to cancel execution.</param>
    /// <returns>The updated subscription details showing cancelled status.</returns>
    [Obsolete("Tenant self-service cancellation is retired until a business rule explicitly defines it.")]
    Task<UserSubscriptionDto> CancelSubscriptionAsync(
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default);
}
