namespace PropertyOS.Domain.UtilityBills.Services;

/// <summary>
/// Pure static domain service for water billing interval calculation.
///
/// Design rules:
///   - No I/O, no DI dependencies — fully unit-testable.
///   - ComputeFromHistory: called ONCE during initial bootstrap only.
///     After that, incremental update is used for every newly discovered bill.
///   - IncrementalUpdate: called once per newly discovered bill.
///     Never reloads historical data — only takes existing scalar statistics
///     and the single new gap measurement.
///   - Historical data remains immutable; this service never re-processes it.
///
/// Incremental average formula:
///   newAvg = (oldAvg × oldCount + newGapDays) / (oldCount + 1)
/// </summary>
public static class WaterBillingIntervalService
{
    // ── Initial bootstrap ────────────────────────────────────────────────────

    /// <summary>
    /// Calculates initial billing interval statistics from a list of historical
    /// bill dates. Called exactly ONCE per water account, during bootstrap.
    ///
    /// Requires at least 2 bills to calculate any interval.
    /// Returns (AvgDays: null, SampleCount: 0) when fewer than 2 bills exist.
    ///
    /// The returned sampleCount is the number of *intervals* (= billCount - 1),
    /// not the number of bills. This is what is stored in billing_interval_sample_count.
    /// </summary>
    /// <param name="billDates">Historical bill dates in any order. Will be sorted internally.</param>
    public static (short? AvgDays, short SampleCount) ComputeFromHistory(
        IReadOnlyList<DateOnly> billDates)
    {
        if (billDates == null || billDates.Count < 2)
            return (null, 0);

        var sorted = billDates.OrderBy(d => d).ToList();

        int totalDays = 0;
        int count     = 0;

        for (int i = 1; i < sorted.Count; i++)
        {
            int gap = sorted[i].DayNumber - sorted[i - 1].DayNumber;
            if (gap > 0) // guard against duplicate dates
            {
                totalDays += gap;
                count++;
            }
        }

        if (count == 0)
            return (null, 0);

        double avg    = (double)totalDays / count;
        short avgDays = (short)Math.Round(avg, MidpointRounding.AwayFromZero);

        return (avgDays, (short)count);
    }

    // ── Incremental update ───────────────────────────────────────────────────

    /// <summary>
    /// Updates interval statistics incrementally after a single newly discovered bill.
    /// Never loads or processes historical bill records — only uses scalar inputs.
    ///
    /// Formula: newAvg = (oldAvg × oldCount + newGapDays) / (oldCount + 1)
    /// </summary>
    /// <param name="oldAvgDays">Current stored average (must be > 0).</param>
    /// <param name="oldCount">Current stored sample count.</param>
    /// <param name="newGapDays">Days between the previous known bill and the newly discovered bill.</param>
    public static (short NewAvgDays, short NewCount) IncrementalUpdate(
        short oldAvgDays,
        short oldCount,
        int newGapDays)
    {
        if (newGapDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(newGapDays),
                "Gap between consecutive bills must be positive.");
        if (oldCount < 0)
            throw new ArgumentOutOfRangeException(nameof(oldCount),
                "Sample count must be non-negative.");

        // When count=0 (no prior observations), the new gap becomes the first average.
        double newAvg   = oldCount == 0
            ? newGapDays
            : ((double)oldAvgDays * oldCount + newGapDays) / (oldCount + 1);

        short newAvgDays = (short)Math.Round(newAvg, MidpointRounding.AwayFromZero);
        short newCount   = (short)(oldCount + 1);

        return (newAvgDays, newCount);
    }

    // ── Next-check scheduling helpers ────────────────────────────────────────

    /// <summary>
    /// Computes the estimated next bill date from the last known bill date
    /// and the stored average billing interval.
    /// </summary>
    public static DateOnly ComputeEstimatedNextBillDate(
        DateOnly lastKnownBillDate,
        short avgIntervalDays)
    {
        if (avgIntervalDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(avgIntervalDays));

        return lastKnownBillDate.AddDays(avgIntervalDays);
    }

    /// <summary>
    /// Computes the NextCheckAt timestamp from the estimated next bill date.
    /// NextCheckAt = EstimatedNextBillDate - safetyLeadDays, converted to UTC.
    /// </summary>
    public static DateTimeOffset ComputeNextCheckAt(
        DateOnly estimatedNextBillDate,
        short safetyLeadDays,
        TimeZoneInfo localTimeZone)
    {
        if (safetyLeadDays < 0)
            throw new ArgumentOutOfRangeException(nameof(safetyLeadDays));

        DateOnly checkDate        = estimatedNextBillDate.AddDays(-safetyLeadDays);
        // Schedule for 06:00 in the local time zone (matches water cron trigger)
        DateTime localCheckTime   = checkDate.ToDateTime(new TimeOnly(6, 0, 0));
        DateTimeOffset withOffset = new DateTimeOffset(
            localCheckTime,
            localTimeZone.GetUtcOffset(localCheckTime));

        return withOffset.ToUniversalTime();
    }
}
