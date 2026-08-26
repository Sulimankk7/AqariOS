namespace PropertyOS.Application.UtilityBills.Services;

/// <summary>
/// The result of a provider bill fetch attempt.
/// IsSuccess = false when the provider is unavailable, returns an error, or times out.
/// Bills may be empty on success if no new bills exist.
/// </summary>
/// <param name="IsSuccess">
///   True if the provider responded successfully.
///   False for all error conditions: HTTP errors, timeouts, parse failures,
///   rate limiting, or provider-not-configured (null provider).
/// </param>
/// <param name="ErrorCode">
///   Short categorized error code for logging and retry logic.
///   Examples: "TIMEOUT", "HTTP_5XX", "PARSE_ERROR", "RATE_LIMITED",
///             "PROVIDER_NOT_CONFIGURED".
///   Never contains credentials, tokens, or sensitive provider data.
/// </param>
/// <param name="ErrorMessage">
///   Human-readable error description for diagnostics.
///   Must be safe to log. Never include credentials, session cookies, or
///   account numbers in their raw form.
/// </param>
/// <param name="Bills">
///   Provider-confirmed bills. Empty when IsSuccess=false or no new bills exist.
/// </param>
/// <param name="TotalOutstandingBalance">
///   Provider-confirmed account balance at fetch time. Null when unavailable.
/// </param>
public sealed record ProviderBillFetchResult(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<ProviderBillRecord> Bills,
    decimal? TotalOutstandingBalance)
{
    /// <summary>Convenience factory for a successful result with bills.</summary>
    public static ProviderBillFetchResult Success(
        IReadOnlyList<ProviderBillRecord> bills,
        decimal? totalOutstandingBalance = null)
        => new(true, null, null, bills, totalOutstandingBalance);

    /// <summary>Convenience factory for a failed result.</summary>
    public static ProviderBillFetchResult Failure(string errorCode, string? errorMessage = null)
        => new(false, errorCode, errorMessage, Array.Empty<ProviderBillRecord>(), null);
}
