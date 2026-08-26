using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Infrastructure.UtilityBills.Providers;

/// <summary>
/// Thread-safe rate limiter and concurrency regulator for external utility providers.
///
/// Ensures that:
///   1. Provider concurrency never exceeds MaxConcurrency (e.g. 2 for Electricity, 1 for Water).
///   2. Outgoing requests per minute (RPM) never exceed RequestsPerMinute (e.g. 10 for Electricity, 5 for Water).
///   3. Electricity and Water operate with independent limits and semaphores.
///   4. Bounded execution prevents request storms or accidental Denial-of-Service against provider portals.
/// </summary>
public sealed class UtilityProviderRateLimiter : IDisposable
{
    private readonly IOptionsMonitor<UtilityBillsOptions> _options;
    private readonly ILogger<UtilityProviderRateLimiter> _logger;

    private readonly SemaphoreSlim _electricitySemaphore;
    private readonly SemaphoreSlim _waterSemaphore;

    private readonly ConcurrentQueue<DateTimeOffset> _electricityRequestTimestamps = new();
    private readonly ConcurrentQueue<DateTimeOffset> _waterRequestTimestamps = new();

    private readonly object _electricitySyncRoot = new();
    private readonly object _waterSyncRoot = new();

    public UtilityProviderRateLimiter(
        IOptionsMonitor<UtilityBillsOptions> options,
        ILogger<UtilityProviderRateLimiter> logger)
    {
        _options = options;
        _logger = logger;

        var initialOpts = _options.CurrentValue;
        _electricitySemaphore = new SemaphoreSlim(
            Math.Max(1, initialOpts.Electricity.MaxConcurrency),
            Math.Max(1, initialOpts.Electricity.MaxConcurrency));

        _waterSemaphore = new SemaphoreSlim(
            Math.Max(1, initialOpts.Water.MaxConcurrency),
            Math.Max(1, initialOpts.Water.MaxConcurrency));
    }

    /// <summary>
    /// Executes the provider action under concurrency and rate limits.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        UtilityType utilityType,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var semaphore = utilityType == UtilityType.Electricity
            ? _electricitySemaphore
            : _waterSemaphore;

        var maxRpm = utilityType == UtilityType.Electricity
            ? _options.CurrentValue.Electricity.RequestsPerMinute
            : _options.CurrentValue.Water.RequestsPerMinute;

        // Step 1: Wait for concurrency slot
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            // Step 2: Enforce Requests Per Minute window
            await EnforceRateLimitAsync(utilityType, maxRpm, cancellationToken);

            // Step 3: Execute provider action
            return await action(cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task EnforceRateLimitAsync(
        UtilityType utilityType,
        int maxRpm,
        CancellationToken cancellationToken)
    {
        if (maxRpm <= 0) return;

        var queue = utilityType == UtilityType.Electricity
            ? _electricityRequestTimestamps
            : _waterRequestTimestamps;

        var syncRoot = utilityType == UtilityType.Electricity
            ? _electricitySyncRoot
            : _waterSyncRoot;

        TimeSpan delayNeeded = TimeSpan.Zero;

        lock (syncRoot)
        {
            var now = DateTimeOffset.UtcNow;
            var windowStart = now.AddMinutes(-1);

            // Purge timestamps older than 1 minute
            while (queue.TryPeek(out var oldest) && oldest < windowStart)
            {
                queue.TryDequeue(out _);
            }

            if (queue.Count >= maxRpm)
            {
                if (queue.TryPeek(out var oldest))
                {
                    // Delay until the oldest request exits the 1-minute window
                    var releaseTime = oldest.AddMinutes(1);
                    if (releaseTime > now)
                    {
                        delayNeeded = releaseTime - now;
                    }
                }
            }

            if (delayNeeded == TimeSpan.Zero)
            {
                queue.Enqueue(now);
            }
        }

        if (delayNeeded > TimeSpan.Zero)
        {
            _logger.LogDebug(
                "Utility rate limiter ({Type}) throttling for {DelayMs}ms to respect {Rpm} RPM limit.",
                utilityType, (int)delayNeeded.TotalMilliseconds, maxRpm);

            await Task.Delay(delayNeeded, cancellationToken);

            lock (syncRoot)
            {
                queue.Enqueue(DateTimeOffset.UtcNow);
            }
        }
    }

    public void Dispose()
    {
        _electricitySemaphore.Dispose();
        _waterSemaphore.Dispose();
    }
}
