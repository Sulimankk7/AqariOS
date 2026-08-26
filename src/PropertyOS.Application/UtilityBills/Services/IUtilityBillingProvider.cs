using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Services;

/// <summary>
/// Abstraction over a single external utility billing provider.
///
/// Implementations must be:
///   - Isolated from controllers, domain entities, notification handlers, and
///     any synchronous HTTP request path.
///   - Timeout-bounded (configurable via UtilityBillsOptions).
///   - Rate-limited (configurable per provider).
///   - Safe for CancellationToken cancellation.
///   - Fail-closed (return ProviderBillFetchResult.Failure on all error paths).
///   - Credential-safe (never log credentials, cookies, or raw session tokens).
///
/// Implementations must fail closed with PROVIDER_NOT_CONFIGURED when their
/// provider or the internal scraper service is disabled or unconfigured.
/// </summary>
public interface IUtilityBillingProvider
{
    /// <summary>The utility type this provider serves.</summary>
    UtilityType ProviderType { get; }

    /// <summary>
    /// Fetches bills for the specified utility account from the external provider.
    ///
    /// When <paramref name="sinceDate"/> is <c>null</c>:
    ///   Returns the provider's full available bill history.
    ///   This path is used ONLY during the one-time bootstrap of a new account.
    ///   Never call with sinceDate=null on an account that has already been bootstrapped.
    ///
    /// When <paramref name="sinceDate"/> is set:
    ///   Returns only bills strictly newer than that date.
    ///   This is the normal incremental sync path.
    ///
    /// On provider failure, timeout, or rate limiting:
    ///   Returns <see cref="ProviderBillFetchResult.Failure"/> — never throws.
    ///   The caller (SyncUtilityAccountCommandHandler) handles all error paths.
    /// </summary>
    /// <param name="accountNumber">The account/subscription number to query.</param>
    /// <param name="meterNumber">Optional meter number, required by some providers.</param>
    /// <param name="sinceDate">
    ///   null → full history (bootstrap only).
    ///   date → incremental sync (bills after this date only).
    /// </param>
    /// <param name="cancellationToken">Cancellation token from Hangfire job.</param>
    Task<ProviderBillFetchResult> FetchBillsAsync(
        string accountNumber,
        string? meterNumber,
        DateOnly? sinceDate,
        CancellationToken cancellationToken = default);
}
