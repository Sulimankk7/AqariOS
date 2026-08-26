namespace PropertyOS.Application.UtilityBills.Options;

/// <summary>
/// Configuration options for the Utility Bills module.
/// Bound from appsettings.json section "UtilityBills".
/// All values are configurable without code changes.
/// </summary>
public sealed class UtilityBillsOptions
{
    public const string SectionName = "UtilityBills";

    /// <summary>
    /// Threshold after which a stale claim (worker crash) is considered abandoned.
    /// Another worker may then reclaim the account after this many minutes.
    /// </summary>
    public int StaleClaimThresholdMinutes { get; set; } = 10;

    public UtilityScraperServiceOptions ScraperService { get; set; } = new();
    public ElectricityOptions Electricity { get; set; } = new();
    public WaterOptions Water { get; set; } = new();
}

/// <summary>
/// Connection settings for the private Python utility-scraper service.
/// The service owns external provider URLs; AqariOS calls only its fixed internal endpoints.
/// </summary>
public sealed class UtilityScraperServiceOptions
{
    /// <summary>Private-network base URL for the long-running scraper service.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Shared HMAC secret. Supply from a secret store/environment, never source control.</summary>
    public string? SharedSecret { get; set; }

    /// <summary>Maximum normalized response size accepted from the internal service.</summary>
    public int MaxResponseBytes { get; set; } = 2 * 1024 * 1024;
}

/// <summary>Configuration for electricity billing synchronization.</summary>
public sealed class ElectricityOptions
{
    /// <summary>
    /// When false, CheckElectricityBillsJob and BootstrapUtilityAccountJob
    /// exit immediately without calling any provider.
    /// The registered adapter fails closed without calling the internal service.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Maximum number of accounts processed per Hangfire trigger execution.</summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>Milliseconds to wait between processing individual accounts in a batch.</summary>
    public int InterBatchDelayMs { get; set; } = 2000;

    /// <summary>Maximum number of concurrent provider requests per batch execution.</summary>
    public int MaxConcurrency { get; set; } = 2;

    /// <summary>
    /// Account is suspended after this many consecutive provider failures.
    /// Requires manual re-activation.
    /// </summary>
    public int MaxConsecutiveFailuresBeforeSkip { get; set; } = 5;

    /// <summary>Base minutes for exponential backoff on failure. Doubled per consecutive failure.</summary>
    public int BackoffBaseMinutes { get; set; } = 30;

    /// <summary>Maximum hours for exponential backoff (caps the doubling).</summary>
    public int MaxBackoffHours { get; set; } = 24;

    /// <summary>Provider HTTP request timeout in seconds.</summary>
    public int ProviderTimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum provider requests per minute (rate limiting).</summary>
    public int RequestsPerMinute { get; set; } = 10;

    /// <summary>
    /// Hours to wait before the next retry when inside the billing window (days 1–3)
    /// and no bill has been found yet.
    /// </summary>
    public int RetryHoursWithinWindow { get; set; } = 6;

    /// <summary>
    /// Hour of day (Amman local time) at which the electricity billing window opens.
    /// Matches the earliest Hangfire cron trigger. Default: 7 (07:00).
    /// </summary>
    public int BillingWindowStartHour { get; set; } = 7;

}


/// <summary>Configuration for water billing synchronization.</summary>
public sealed class WaterOptions
{
    /// <summary>
    /// When false, CheckWaterBillsJob and BootstrapUtilityAccountJob
    /// exit immediately without calling any provider.
    /// The registered adapter fails closed without calling the internal service.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Maximum number of accounts processed per Hangfire trigger execution.</summary>
    public int BatchSize { get; set; } = 5;

    /// <summary>Milliseconds to wait between processing individual accounts in a batch.</summary>
    public int InterBatchDelayMs { get; set; } = 3000;

    /// <summary>Maximum number of concurrent provider requests per batch execution.</summary>
    public int MaxConcurrency { get; set; } = 1;

    /// <summary>Account is suspended after this many consecutive provider failures.</summary>
    public int MaxConsecutiveFailuresBeforeSkip { get; set; } = 3;

    /// <summary>Base minutes for exponential backoff on failure.</summary>
    public int BackoffBaseMinutes { get; set; } = 60;

    /// <summary>Maximum hours for exponential backoff.</summary>
    public int MaxBackoffHours { get; set; } = 48;

    /// <summary>
    /// Days before the EstimatedNextBillDate to schedule the first check.
    /// Example: avg=29 days, lead=3 → check starts on day 26 after last bill.
    /// </summary>
    public int SafetyLeadDays { get; set; } = 3;

    /// <summary>
    /// Days to wait before rechecking when inside the expected billing window
    /// but no new bill has been found yet.
    /// </summary>
    public int NoNewBillRetryDays { get; set; } = 3;

    /// <summary>
    /// Maximum times to retry within the expected billing window before
    /// skipping ahead to the next estimated window.
    /// </summary>
    public int MaxRetriesWithinWindow { get; set; } = 3;

    /// <summary>
    /// Fallback interval (days) for accounts with insufficient historical data
    /// (fewer than 2 bills — cannot calculate an average).
    /// </summary>
    public int DefaultCheckIntervalDays { get; set; } = 30;

    /// <summary>Provider HTTP request timeout in seconds.</summary>
    public int ProviderTimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum provider requests per minute (rate limiting).</summary>
    public int RequestsPerMinute { get; set; } = 5;

}
