using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Application.Subscriptions;

/// <summary>
/// Pure PAYG calendar arithmetic. Every interval is half-open: [start, end).
/// The yearly unit rate is a monthly-equivalent rate and is multiplied by 12
/// before calendar-year tenant-day proration.
/// </summary>
public static class PaygUsageCalculator
{
    public static (DateOnly Start, DateOnly End) GetCalendarPeriod(DateOnly businessDate, BillingCycleEnum cycle) =>
        cycle == BillingCycleEnum.Monthly
            ? (new DateOnly(businessDate.Year, businessDate.Month, 1), new DateOnly(businessDate.Year, businessDate.Month, 1).AddMonths(1))
            : (new DateOnly(businessDate.Year, 1, 1), new DateOnly(businessDate.Year + 1, 1, 1));

    public static int Days(DateOnly start, DateOnly end) => Math.Max(0, end.DayNumber - start.DayNumber);

    public static decimal CalculateAmount(
        int leaseDays,
        int periodDays,
        decimal monthlyEquivalentUnitPrice,
        BillingCycleEnum cycle,
        bool isChargeable)
    {
        if (!isChargeable || leaseDays <= 0) return 0m;
        if (periodDays <= 0) throw new ArgumentOutOfRangeException(nameof(periodDays));
        var cycleUnitPrice = cycle == BillingCycleEnum.Yearly
            ? monthlyEquivalentUnitPrice * 12m
            : monthlyEquivalentUnitPrice;
        return Math.Round(cycleUnitPrice * leaseDays / periodDays, 3, MidpointRounding.AwayFromZero);
    }

    public static bool AccruesUsage(SubscriptionStatusEnum status) =>
        status is SubscriptionStatusEnum.Trialing or SubscriptionStatusEnum.Active or SubscriptionStatusEnum.PastDue;

    public static bool IsChargeable(SubscriptionStatusEnum status) =>
        status is SubscriptionStatusEnum.Active or SubscriptionStatusEnum.PastDue;

    public static (DateOnly Start, DateOnly End)? GetBillableInterval(
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly calculatedThrough,
        DateOnly subscriptionStart,
        DateOnly subscriptionEnd,
        DateOnly leaseStart,
        DateOnly leaseEnd,
        DateOnly activationDate,
        DateOnly? lifecycleExitDate = null,
        params DateOnly?[] deletionDates)
    {
        var start = new[] { periodStart, subscriptionStart, leaseStart, activationDate }.Max();
        var end = new[] { periodEnd, calculatedThrough, subscriptionEnd, leaseEnd }.Min();
        if (lifecycleExitDate.HasValue && lifecycleExitDate.Value < end) end = lifecycleExitDate.Value;
        foreach (var deletedOn in deletionDates)
            if (deletedOn.HasValue && deletedOn.Value < end) end = deletedOn.Value;
        return end > start ? (start, end) : null;
    }
}
