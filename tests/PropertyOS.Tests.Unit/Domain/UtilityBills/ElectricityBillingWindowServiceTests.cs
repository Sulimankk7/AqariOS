using System;
using PropertyOS.Domain.UtilityBills.Services;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.UtilityBills;

public class ElectricityBillingWindowServiceTests
{
    private static readonly TimeZoneInfo AmmanTz =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Jordan Standard Time" : "Asia/Amman");

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(15, false)]
    [InlineData(31, false)]
    public void IsWithinBillingWindow_DetectsDay1To3(int day, bool expected)
    {
        var date = new DateOnly(2026, 5, day);
        Assert.Equal(expected, ElectricityBillingWindowService.IsWithinBillingWindow(date));
    }

    [Fact]
    public void ComputeNextCheckAt_WhenBillFound_SchedulesForFirstOfNextMonth()
    {
        var refDate = new DateOnly(2026, 5, 2);
        var refTime = new DateTimeOffset(2026, 5, 2, 7, 0, 0, TimeSpan.FromHours(3));

        var nextCheck = ElectricityBillingWindowService.ComputeNextCheckAt(
            referenceDate: refDate,
            referenceTime: refTime,
            newBillFound: true,
            retryHoursWithinWindow: 6,
            billingWindowStartHour: 7,
            localTimeZone: AmmanTz);

        // Next check should be in June 2026
        var localNext = TimeZoneInfo.ConvertTime(nextCheck, AmmanTz);
        Assert.Equal(2026, localNext.Year);
        Assert.Equal(6, localNext.Month);
        Assert.Equal(1, localNext.Day);
        Assert.Equal(7, localNext.Hour);
    }

    [Fact]
    public void ComputeNextCheckAt_WhenInWindowAndNoBillFound_AddsRetryHours()
    {
        var refDate = new DateOnly(2026, 5, 1);
        var refTime = new DateTimeOffset(2026, 5, 1, 7, 0, 0, TimeSpan.FromHours(3));

        var nextCheck = ElectricityBillingWindowService.ComputeNextCheckAt(
            referenceDate: refDate,
            referenceTime: refTime,
            newBillFound: false,
            retryHoursWithinWindow: 6,
            billingWindowStartHour: 7,
            localTimeZone: AmmanTz);

        Assert.Equal(refTime.AddHours(6), nextCheck);
    }

    [Fact]
    public void ComputeNextCheckAt_WhenAfterWindowAndNoBill_SchedulesForFirstOfNextMonth()
    {
        var refDate = new DateOnly(2026, 5, 4);
        var refTime = new DateTimeOffset(2026, 5, 4, 12, 0, 0, TimeSpan.FromHours(3));

        var nextCheck = ElectricityBillingWindowService.ComputeNextCheckAt(
            referenceDate: refDate,
            referenceTime: refTime,
            newBillFound: false,
            retryHoursWithinWindow: 6,
            billingWindowStartHour: 7,
            localTimeZone: AmmanTz);

        var localNext = TimeZoneInfo.ConvertTime(nextCheck, AmmanTz);
        Assert.Equal(2026, localNext.Year);
        Assert.Equal(6, localNext.Month);
        Assert.Equal(1, localNext.Day);
    }

    [Fact]
    public void ComputeNextCheckAt_DecemberRollsOverToJanuaryNextYear()
    {
        var refDate = new DateOnly(2026, 12, 5);
        var refTime = new DateTimeOffset(2026, 12, 5, 12, 0, 0, TimeSpan.FromHours(3));

        var nextCheck = ElectricityBillingWindowService.ComputeNextCheckAt(
            referenceDate: refDate,
            referenceTime: refTime,
            newBillFound: true,
            retryHoursWithinWindow: 6,
            billingWindowStartHour: 7,
            localTimeZone: AmmanTz);

        var localNext = TimeZoneInfo.ConvertTime(nextCheck, AmmanTz);
        Assert.Equal(2027, localNext.Year);
        Assert.Equal(1, localNext.Month);
        Assert.Equal(1, localNext.Day);
    }
}
