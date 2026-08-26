using System;
using System.Collections.Generic;
using PropertyOS.Domain.UtilityBills.Services;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.UtilityBills;

public class WaterBillingIntervalServiceAdvancedTests
{
    [Fact]
    public void ComputeFromHistory_WithVaryingRealWorldDates_CalculatesCorrectAverage()
    {
        // 5 real-world water bill dates:
        // 2025-11-20 -> 2025-12-18 = 28 days
        // 2025-12-18 -> 2026-01-21 = 34 days
        // 2026-01-21 -> 2026-02-18 = 28 days
        // 2026-02-18 -> 2026-03-15 = 25 days
        // Gaps: 28, 34, 28, 25. Total = 115 days across 4 gaps.
        // Average = 115 / 4 = 28.75 -> rounded to 29 days.
        var dates = new List<DateOnly>
        {
            new(2025, 11, 20),
            new(2025, 12, 18),
            new(2026, 1, 21),
            new(2026, 2, 18),
            new(2026, 3, 15)
        };

        var (avgDays, sampleCount) = WaterBillingIntervalService.ComputeFromHistory(dates);

        Assert.Equal((short)29, avgDays);
        Assert.Equal((short)4, sampleCount);
    }

    [Fact]
    public void IncrementalUpdate_SequenceOfNewBills_MaintainsRunningAverageMathematicallyIdentical()
    {
        // Start with historical average of 29 days across 4 samples (sum = 116)
        short currentAvg = 29;
        short currentCount = 4;

        // New bill arrives 30 days after the last one:
        // (29 * 4 + 30) / 5 = (116 + 30) / 5 = 146 / 5 = 29.2 -> 29
        var (newAvg1, newCount1) = WaterBillingIntervalService.IncrementalUpdate(currentAvg, currentCount, 30);
        Assert.Equal((short)29, newAvg1);
        Assert.Equal((short)5, newCount1);

        // Next new bill arrives 36 days later:
        // (29 * 5 + 36) / 6 = (145 + 36) / 6 = 181 / 6 = 30.16 -> 30
        var (newAvg2, newCount2) = WaterBillingIntervalService.IncrementalUpdate(newAvg1, newCount1, 36);
        Assert.Equal((short)30, newAvg2);
        Assert.Equal((short)6, newCount2);
    }

    [Fact]
    public void ComputeEstimatedNextBillDate_YearBoundaryCrossing_CalculatesCorrectDate()
    {
        var lastBill = new DateOnly(2026, 12, 15);
        short avgInterval = 30;

        var estimated = WaterBillingIntervalService.ComputeEstimatedNextBillDate(lastBill, avgInterval);

        Assert.Equal(new DateOnly(2027, 1, 14), estimated);
    }

    [Fact]
    public void ComputeNextCheckAt_AppliesSafetyLeadDaysAndMorningSlot()
    {
        var estimatedDate = new DateOnly(2026, 4, 15);
        short safetyLeadDays = 3;
        var tz = TimeZoneInfo.Utc;

        var nextCheck = WaterBillingIntervalService.ComputeNextCheckAt(estimatedDate, safetyLeadDays, tz);

        // 2026-04-15 minus 3 days = 2026-04-12 at 06:00 UTC
        Assert.Equal(new DateTimeOffset(2026, 4, 12, 6, 0, 0, TimeSpan.Zero), nextCheck);
    }
}
