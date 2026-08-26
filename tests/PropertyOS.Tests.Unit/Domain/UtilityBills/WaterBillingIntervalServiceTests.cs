using System;
using System.Collections.Generic;
using PropertyOS.Domain.UtilityBills.Services;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.UtilityBills;

public class WaterBillingIntervalServiceTests
{
    [Fact]
    public void ComputeFromHistory_NullOrEmptyList_ReturnsNullAvgAndZeroCount()
    {
        var (avg1, count1) = WaterBillingIntervalService.ComputeFromHistory(null!);
        Assert.Null(avg1);
        Assert.Equal(0, count1);

        var (avg2, count2) = WaterBillingIntervalService.ComputeFromHistory(new List<DateOnly>());
        Assert.Null(avg2);
        Assert.Equal(0, count2);
    }

    [Fact]
    public void ComputeFromHistory_SingleBill_ReturnsNullAvgAndZeroCount()
    {
        var dates = new List<DateOnly> { new DateOnly(2026, 1, 15) };
        var (avg, count) = WaterBillingIntervalService.ComputeFromHistory(dates);

        Assert.Null(avg);
        Assert.Equal(0, count);
    }

    [Fact]
    public void ComputeFromHistory_TwoBills_ReturnsSingleGapCorrectly()
    {
        var dates = new List<DateOnly>
        {
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 30) // 29 days gap
        };

        var (avg, count) = WaterBillingIntervalService.ComputeFromHistory(dates);

        Assert.Equal((short)29, avg);
        Assert.Equal((short)1, count);
    }

    [Fact]
    public void ComputeFromHistory_ThreeBills_CalculatesAverageOfGaps()
    {
        var dates = new List<DateOnly>
        {
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31), // gap = 30
            new DateOnly(2026, 3, 1)   // gap = 29 (2026 is non-leap year: 28 days in Feb -> Jan 31 + 29 = Mar 1)
        };

        var (avg, count) = WaterBillingIntervalService.ComputeFromHistory(dates);

        // Average = (30 + 29) / 2 = 29.5 -> rounded to 30
        Assert.Equal((short)30, avg);
        Assert.Equal((short)2, count);
    }

    [Fact]
    public void ComputeFromHistory_UnsortedInput_SortsDatesCorrectly()
    {
        var dates = new List<DateOnly>
        {
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31)
        };

        var (avg, count) = WaterBillingIntervalService.ComputeFromHistory(dates);

        Assert.Equal((short)30, avg);
        Assert.Equal((short)2, count);
    }

    [Fact]
    public void IncrementalUpdate_FirstObservation_SetsGapAsFirstAverage()
    {
        var (newAvg, newCount) = WaterBillingIntervalService.IncrementalUpdate(
            oldAvgDays: 0,
            oldCount: 0,
            newGapDays: 28);

        Assert.Equal((short)28, newAvg);
        Assert.Equal((short)1, newCount);
    }

    [Fact]
    public void IncrementalUpdate_RunningAverage_UpdatesAccuratelyWithoutHistory()
    {
        // Old average: 30 days across 2 observations. New gap: 33 days.
        // New average: (30 * 2 + 33) / 3 = 93 / 3 = 31.
        var (newAvg, newCount) = WaterBillingIntervalService.IncrementalUpdate(
            oldAvgDays: 30,
            oldCount: 2,
            newGapDays: 33);

        Assert.Equal((short)31, newAvg);
        Assert.Equal((short)3, newCount);
    }

    [Fact]
    public void IncrementalUpdate_InvalidGapOrCount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WaterBillingIntervalService.IncrementalUpdate(30, 2, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WaterBillingIntervalService.IncrementalUpdate(30, -1, 30));
    }

    [Fact]
    public void ComputeEstimatedNextBillDate_AddsIntervalToLastBillDate()
    {
        var lastBill = new DateOnly(2026, 4, 1);
        var estimated = WaterBillingIntervalService.ComputeEstimatedNextBillDate(lastBill, 30);

        Assert.Equal(new DateOnly(2026, 5, 1), estimated);
    }

    [Fact]
    public void ComputeNextCheckAt_SubtractsSafetyLeadDays()
    {
        var estimated = new DateOnly(2026, 5, 10);
        var tz = TimeZoneInfo.Utc;

        var nextCheck = WaterBillingIntervalService.ComputeNextCheckAt(estimated, 3, tz);

        // 2026-05-10 minus 3 days = 2026-05-07 at 06:00 UTC
        Assert.Equal(new DateTimeOffset(2026, 5, 7, 6, 0, 0, TimeSpan.Zero), nextCheck);
    }
}
