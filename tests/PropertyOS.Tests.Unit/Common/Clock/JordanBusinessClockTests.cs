using System;
using PropertyOS.Infrastructure.Common.Clock;
using Xunit;

namespace PropertyOS.Tests.Unit.Common.Clock;

public class JordanBusinessClockTests
{
    [Fact]
    public void GetJordanBusinessDate_UtcInstantNearMidnight_ResolvesToCorrectJordanCalendarDate()
    {
        var clock = new JordanBusinessClock();

        // 2026-08-01 00:30 Jordan time is 2026-07-31 21:30 UTC (Jordan is UTC+3 in summer / standard time)
        var utcInstant = new DateTimeOffset(2026, 7, 31, 21, 30, 0, TimeSpan.Zero);

        var jordanDate = clock.GetJordanBusinessDate(utcInstant);

        // Expect Jordan business calendar date to be 2026-08-01 even though UTC date is 2026-07-31
        Assert.Equal(new DateOnly(2026, 8, 1), jordanDate);
    }
}
