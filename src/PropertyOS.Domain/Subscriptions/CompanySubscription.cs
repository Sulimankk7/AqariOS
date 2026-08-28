using System;
using PropertyOS.Domain.Subscriptions.Enums;
using PropertyOS.Domain.Companies;

namespace PropertyOS.Domain.Subscriptions;

public class CompanySubscription
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatusEnum Status { get; set; } = SubscriptionStatusEnum.Trialing;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly? TrialEndDate { get; set; }
    public decimal PriceAtSubscription { get; set; }
    public string CurrencyAtSubscription { get; set; } = "JOD";
    public BillingCycleEnum BillingCycle { get; set; } = BillingCycleEnum.Monthly;
    public bool AutoRenew { get; set; } = true;
    public DateTimeOffset? SuspendedAt { get; set; }
    public string? SuspensionReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }
    public string? ExternalBillingRef { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Company Company { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;

    public void ApplyApprovedPlanChange(
        Guid planId,
        BillingCycleEnum billingCycle,
        decimal priceAtSubscription,
        string currencyAtSubscription,
        DateTimeOffset changedAt)
    {
        PlanId = planId;
        BillingCycle = billingCycle;
        PriceAtSubscription = priceAtSubscription;
        CurrencyAtSubscription = currencyAtSubscription;
        UpdatedAt = changedAt;
    }
}
