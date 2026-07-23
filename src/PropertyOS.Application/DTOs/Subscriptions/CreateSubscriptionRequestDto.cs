using System;
using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Application.DTOs.Subscriptions;

/// <summary>
/// Request payload for subscribing to a plan.
/// </summary>
public class CreateSubscriptionRequestDto
{
    /// <summary>
    /// Identifier of the target subscription plan.
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    /// Selected billing cycle (Monthly or Yearly).
    /// </summary>
    public BillingCycleEnum BillingCycle { get; set; } = BillingCycleEnum.Monthly;
}
