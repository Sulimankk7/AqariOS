namespace PropertyOS.Domain.UtilityBills.Services;

/// <summary>
/// Pure static domain service for electricity billing window logic.
///
/// Design rules:
///   - No I/O, no DI dependencies — fully unit-testable.
///   - Electricity bills are expected during days 1–3 of each month only.
///   - The scheduler runs 3 cron triggers per eligible day (07:00, 09:00, 11:00 Amman).
///   - This service provides all date/NextCheckAt decisions so the command handler
///     stays free of raw DateTime arithmetic.
/// </summary>
public static class ElectricityBillingWindowService
{
    /// <summary>The first day of the electricity billing window (inclusive).</summary>
    public const int WindowStartDay = 1;

    /// <summary>The last day of the electricity billing window (inclusive).</summary>
    public const int WindowEndDay = 3;

    // ── Window detection ─────────────────────────────────────────────────────

    /// <summary>Returns true when the given date falls within the billing window (days 1–3).</summary>
    public static bool IsWithinBillingWindow(DateOnly date)
        => date.Day >= WindowStartDay && date.Day <= WindowEndDay;

    // ── NextCheckAt calculation ──────────────────────────────────────────────

    /// <summary>
    /// Computes the NextCheckAt for an electricity account based on the current
    /// date and whether a bill was found in this sync cycle.
    ///
    /// Rules:
    ///   - Bill found in any context:
    ///       → 1st of next month at the billing window start hour (Amman tz)
    ///   - Within window (day 1–3), no bill yet:
    ///       → now + retryHoursWithinWindow (default 6h)
    ///         This keeps the account eligible for the next cron slot.
    ///   - After window (day 4+), no bill found:
    ///       → 1st of next month at the billing window start hour (Amman tz)
    ///         No further checks this month.
    ///   - Before day 1 (i.e., account created mid-month):
    ///       → 1st of current month at the billing window start hour (Amman tz)
    ///         If current month's day 1 is in the past, go to next month.
    /// </summary>
    /// <param name="referenceDate">The date on which this calculation is being made.</param>
    /// <param name="referenceTime">The time on which this calculation is being made (UTC).</param>
    /// <param name="newBillFound">Whether a new bill was discovered in this sync.</param>
    /// <param name="retryHoursWithinWindow">
    ///   Hours to wait before the next retry when inside the window and no bill yet.
    ///   Must be configurable; defaults to 6.
    /// </param>
    /// <param name="billingWindowStartHour">
    ///   The hour of day (local Amman time) at which the billing window opens.
    ///   Matches the earliest Hangfire cron trigger. Default: 7.
    /// </param>
    /// <param name="localTimeZone">The local timezone (Asia/Amman).</param>
    public static DateTimeOffset ComputeNextCheckAt(
        DateOnly referenceDate,
        DateTimeOffset referenceTime,
        bool newBillFound,
        int retryHoursWithinWindow,
        int billingWindowStartHour,
        TimeZoneInfo localTimeZone)
    {
        if (newBillFound || !IsWithinBillingWindow(referenceDate))
        {
            // Next check is on the 1st of next month
            return FirstOfNextMonthAt(referenceDate, billingWindowStartHour, localTimeZone);
        }

        // Inside window but no bill yet → retry after configured interval
        return referenceTime.AddHours(retryHoursWithinWindow);
    }

    /// <summary>
    /// Returns the NextCheckAt for a newly linked electricity account.
    /// If today is within the billing window (days 1-3), schedule immediately.
    /// Otherwise schedule for the 1st of next month.
    /// </summary>
    public static DateTimeOffset ComputeInitialNextCheckAt(
        DateOnly today,
        DateTimeOffset now,
        int billingWindowStartHour,
        TimeZoneInfo localTimeZone)
    {
        if (IsWithinBillingWindow(today))
            return now; // Bootstrap job will set next_check_at after historical import

        return FirstOfNextMonthAt(today, billingWindowStartHour, localTimeZone);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static DateTimeOffset FirstOfNextMonthAt(
        DateOnly referenceDate,
        int hour,
        TimeZoneInfo localTimeZone)
    {
        int year  = referenceDate.Month == 12 ? referenceDate.Year + 1 : referenceDate.Year;
        int month = referenceDate.Month == 12 ? 1 : referenceDate.Month + 1;

        DateTime localDt   = new DateTime(year, month, 1, hour, 0, 0, DateTimeKind.Unspecified);
        DateTimeOffset dto = new DateTimeOffset(localDt, localTimeZone.GetUtcOffset(localDt));
        return dto.ToUniversalTime();
    }
}
