using FluentAssertions;
using PropertyOS.Application.Subscriptions;
using PropertyOS.Domain.Subscriptions;
using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Tests.Unit.Application.Subscriptions;

public sealed class PaygUsageCalculatorTests
{
    [Fact]
    public void MonthlyFullPeriod_UsesActualCalendarDays()
    {
        var (start, end) = PaygUsageCalculator.GetCalendarPeriod(new DateOnly(2026, 8, 15), BillingCycleEnum.Monthly);
        var days = PaygUsageCalculator.Days(start, end);

        days.Should().Be(31);
        PaygUsageCalculator.CalculateAmount(days, days, 3m, BillingCycleEnum.Monthly, true).Should().Be(3m);
    }

    [Fact]
    public void MidMonthActivation_IsProratedByLeaseDays()
    {
        PaygUsageCalculator.CalculateAmount(15, 30, 3m, BillingCycleEnum.Monthly, true)
            .Should().Be(1.5m);
    }

    [Fact]
    public void ActivationAndTermination_ProduceOnlyTheActiveHalfOpenInterval()
    {
        var interval = PaygUsageCalculator.GetBillableInterval(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1),
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1),
            new DateOnly(2026, 8, 1), new DateOnly(2026, 12, 1),
            new DateOnly(2026, 8, 15), new DateOnly(2026, 8, 25));

        interval.Should().Be((new DateOnly(2026, 8, 15), new DateOnly(2026, 8, 25)));
        PaygUsageCalculator.Days(interval!.Value.Start, interval.Value.End).Should().Be(10);
    }

    [Fact]
    public void MissingTerminationDoesNotUseDateOnlyMinValue_AndActualTerminationCapsTheInterval()
    {
        var withoutTermination = PaygUsageCalculator.GetBillableInterval(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 17),
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1),
            new DateOnly(2026, 8, 1), new DateOnly(2027, 1, 1), new DateOnly(2026, 8, 1), null);
        var withTermination = PaygUsageCalculator.GetBillableInterval(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 17),
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1),
            new DateOnly(2026, 8, 1), new DateOnly(2027, 1, 1), new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 8));

        withoutTermination.Should().Be((new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 17)));
        withTermination.Should().Be((new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 8)));
    }

    [Fact]
    public void SoftDeletion_StopsUsageOnTheDeletionBoundary()
    {
        var interval = PaygUsageCalculator.GetBillableInterval(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1),
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1),
            new DateOnly(2026, 8, 1), new DateOnly(2026, 12, 1),
            new DateOnly(2026, 8, 1), null, new DateOnly(2026, 8, 20));

        PaygUsageCalculator.Days(interval!.Value.Start, interval.Value.End).Should().Be(19);
    }

    [Fact]
    public void MultipleLeases_AreAggregatedIndependently()
    {
        var first = PaygUsageCalculator.GetBillableInterval(
            new(2026, 8, 1), new(2026, 9, 1), new(2026, 9, 1), new(2026, 1, 1), new(2027, 1, 1),
            new(2026, 8, 1), new(2026, 12, 1), new(2026, 8, 1));
        var second = PaygUsageCalculator.GetBillableInterval(
            new(2026, 8, 1), new(2026, 9, 1), new(2026, 9, 1), new(2026, 1, 1), new(2027, 1, 1),
            new(2026, 8, 1), new(2026, 12, 1), new(2026, 8, 16));

        (PaygUsageCalculator.Days(first!.Value.Start, first.Value.End)
         + PaygUsageCalculator.Days(second!.Value.Start, second.Value.End)).Should().Be(47);
    }

    [Fact]
    public void FebruaryAndLeapYear_UseActualPeriodLength()
    {
        var february = PaygUsageCalculator.GetCalendarPeriod(new DateOnly(2028, 2, 10), BillingCycleEnum.Monthly);
        var year = PaygUsageCalculator.GetCalendarPeriod(new DateOnly(2028, 6, 1), BillingCycleEnum.Yearly);

        PaygUsageCalculator.Days(february.Start, february.End).Should().Be(29);
        PaygUsageCalculator.Days(year.Start, year.End).Should().Be(366);
    }

    [Fact]
    public void YearlyRate_IsMonthlyEquivalentMultipliedByTwelveAndProratedByYearDays()
    {
        PaygUsageCalculator.CalculateAmount(366, 366, 2m, BillingCycleEnum.Yearly, true)
            .Should().Be(24m);
    }

    [Theory]
    [InlineData(SubscriptionStatusEnum.Trialing, true, false)]
    [InlineData(SubscriptionStatusEnum.Active, true, true)]
    [InlineData(SubscriptionStatusEnum.PastDue, true, true)]
    [InlineData(SubscriptionStatusEnum.Suspended, false, false)]
    [InlineData(SubscriptionStatusEnum.Cancelled, false, false)]
    [InlineData(SubscriptionStatusEnum.Expired, false, false)]
    public void SubscriptionStateRules_AreExplicit(
        SubscriptionStatusEnum status, bool accruesUsage, bool isChargeable)
    {
        PaygUsageCalculator.AccruesUsage(status).Should().Be(accruesUsage);
        PaygUsageCalculator.IsChargeable(status).Should().Be(isChargeable);
    }

    [Fact]
    public void TrialUsage_IsRecordedWithoutCharge()
    {
        PaygUsageCalculator.CalculateAmount(20, 31, 3m, BillingCycleEnum.Monthly, false)
            .Should().Be(0m);
    }

    [Fact]
    public void SameUsageCalculation_IsDeterministicAndIdempotent()
    {
        var first = PaygUsageCalculator.CalculateAmount(47, 31, 3m, BillingCycleEnum.Monthly, true);
        var second = PaygUsageCalculator.CalculateAmount(47, 31, 3m, BillingCycleEnum.Monthly, true);

        second.Should().Be(first);
    }

    [Fact]
    public void UsagePeriod_RetainsItsBeginningOfPeriodPriceSnapshot()
    {
        var period = PaygUsagePeriod.Create(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1),
            BillingCycleEnum.Monthly, 3m, "JOD", true, DateTimeOffset.UtcNow);
        var laterPlanRate = 4m;

        var historicalAmount = PaygUsageCalculator.CalculateAmount(
            31, period.PeriodDayCount, period.MonthlyEquivalentUnitPriceSnapshot,
            period.BillingCycle, period.IsChargeable);

        laterPlanRate.Should().NotBe(period.MonthlyEquivalentUnitPriceSnapshot);
        historicalAmount.Should().Be(3m);
    }

    [Fact]
    public void FinalizedPeriod_DoesNotChangeWhenRecalculationIsAttempted()
    {
        var period = PaygUsagePeriod.Create(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1),
            BillingCycleEnum.Monthly, 3m, "JOD", true, DateTimeOffset.UtcNow);
        period.UpdateTotals(2, 62, 6m, 6m, new DateOnly(2026, 9, 1), DateTimeOffset.UtcNow);
        period.Finalize(DateTimeOffset.UtcNow);

        period.UpdateTotals(99, 999, 999m, 999m, new DateOnly(2026, 9, 1), DateTimeOffset.UtcNow);

        period.ActiveLeaseCount.Should().Be(2);
        period.AccumulatedLeaseDays.Should().Be(62);
        period.EstimatedAmount.Should().Be(6m);
    }
}
