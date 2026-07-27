using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Infrastructure.Common.Clock;

/// <summary>
/// Production IBusinessClock implementation using the Jordan business calendar.
///
/// TIMEZONE RESOLUTION (in priority order):
///   1. Asia/Amman        — IANA identifier used on Linux / macOS / .NET with tzdata.
///   2. Jordan Standard Time — Windows timezone identifier.
///   If neither resolves, the constructor throws InvalidOperationException to
///   fail fast with a clear diagnostic message rather than silently using a
///   fixed UTC+3 fallback (which would mask misconfigured deployments and could
///   produce incorrect dates if Jordan's timezone rules ever change).
///
/// FAIL-FAST BEHAVIOR:
///   The service is registered as Singleton, so a missing timezone data package
///   surfaces immediately at application startup rather than during a scheduled
///   sweep. On Linux containers, ensure the 'tzdata' package is installed.
///   On Windows, ensure the OS timezone database is current.
///
/// UTC CONVENTION:
///   UtcNow returns the current UTC wall-clock instant (DateTimeOffset).
///   Mutation/audit timestamps (UpdatedAt, ChangedAt, etc.) MUST use UTC.
///   Only the lease expiration eligibility decision uses the Jordan calendar date.
/// </summary>
public class JordanBusinessClock : IBusinessClock
{
    private static readonly TimeZoneInfo JordanTimeZone = ResolveJordanTimeZone();

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <summary>
    /// Converts the given UTC instant (or the current instant if null) to the
    /// Asia/Amman calendar date. This is used to determine whether a lease
    /// contract's EndDate has been reached according to Jordan business time.
    /// </summary>
    public DateOnly GetJordanBusinessDate(DateTimeOffset? utcInstant = null)
    {
        var instant = utcInstant ?? UtcNow;
        var jordanTime = TimeZoneInfo.ConvertTime(instant, JordanTimeZone);
        return DateOnly.FromDateTime(jordanTime.DateTime);
    }

    private static TimeZoneInfo ResolveJordanTimeZone()
    {
        // Attempt 1: IANA identifier (Linux/macOS/.NET with IANA timezone data)
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman");
        }
        catch (TimeZoneNotFoundException) { }

        // Attempt 2: Windows timezone identifier
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
        }
        catch (TimeZoneNotFoundException) { }

        // Neither identifier resolved — fail fast.
        // Do NOT create a fixed UTC+3 fallback: it hides misconfigured deployments
        // and will produce incorrect dates if Jordan's timezone rules change.
        throw new InvalidOperationException(
            "Jordan timezone data could not be resolved. " +
            "Tried: 'Asia/Amman' (Linux/macOS/IANA) and 'Jordan Standard Time' (Windows). " +
            "On Linux containers, ensure the 'tzdata' package is installed " +
            "(e.g., 'apt-get install -y tzdata' or 'apk add tzdata'). " +
            "On Windows, ensure the OS timezone database is current.");
    }
}
