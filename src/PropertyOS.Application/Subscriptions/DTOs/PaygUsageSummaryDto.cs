using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Application.Subscriptions.DTOs;

public sealed class PaygUsageSummaryDto
{
    public Guid SubscriptionId { get; init; }
    public Guid PlanId { get; init; }
    public string PlanNameEn { get; init; } = string.Empty;
    public string PlanNameAr { get; init; } = string.Empty;
    public SubscriptionPricingModel PricingModel { get; init; }
    public SubscriptionStatusEnum SubscriptionStatus { get; init; }
    public BillingCycleEnum BillingCycle { get; init; }
    public bool IsPayAsYouGo => PricingModel == SubscriptionPricingModel.PayAsYouGo;
    public bool IsEstimated { get; init; }
    public bool IsFinalized { get; init; }
    public bool IsChargeable { get; init; }
    public int CurrentActiveLeaseCount { get; init; }
    public int AccumulatedLeaseDays { get; init; }
    public decimal MonthlyEquivalentUnitPrice { get; init; }
    public string Currency { get; init; } = "JOD";
    public decimal EstimatedAmount { get; init; }
    public decimal ProjectedPeriodAmount { get; init; }
    public DateOnly BillingPeriodStart { get; init; }
    public DateOnly BillingPeriodEnd { get; init; }
    public DateOnly CalculatedThrough { get; init; }
    public int PeriodDays { get; init; }
    public int DaysElapsed { get; init; }
    public int DaysRemaining { get; init; }
}
