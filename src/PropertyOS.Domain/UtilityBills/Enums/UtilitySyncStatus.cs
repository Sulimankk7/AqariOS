namespace PropertyOS.Domain.UtilityBills.Enums;

/// <summary>
/// The synchronization state of a utility account with its external provider.
/// Maps to the PostgreSQL enum type: utility_sync_status_enum.
///
/// Design rules:
///   - NeverSynced     : account just created; bootstrap not yet completed.
///   - Syncing         : a worker has claimed this account and is currently processing it.
///   - Synced          : last provider call succeeded.
///   - ProviderError   : last provider call returned an HTTP/application-level error.
///   - RateLimited     : provider rejected the request due to rate-limiting.
///   - Timeout         : provider call exceeded the configured timeout.
///   - Suspended       : consecutive_failure_count exceeded the configured maximum;
///                       no further automatic processing until manually re-enabled.
///   - InvalidAccount  : provider confirmed the external account number is invalid.
/// </summary>
public enum UtilitySyncStatus
{
    NeverSynced,
    Syncing,
    Synced,
    ProviderError,
    RateLimited,
    Timeout,
    Suspended,
    InvalidAccount
}
