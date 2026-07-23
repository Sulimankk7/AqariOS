using System;
using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Application.DTOs.Subscriptions;

/// <summary>
/// Request payload for modifying/upgrading/downgrading an existing subscription plan.
/// </summary>
public class ChangePlanRequestDto
{
    /// <summary>
    /// Identifier of the new subscription plan.
    /// </summary>
    public Guid NewPlanId { get; set; }

    /// <summary>
    /// Selected billing cycle (Monthly or Yearly).
    /// </summary>
    public BillingCycleEnum NewBillingCycle { get; set; } = BillingCycleEnum.Monthly;
}
