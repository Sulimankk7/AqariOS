using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Infrastructure.UtilityBills.Providers;

/// <summary>
/// Fail-closed no-op implementation of IUtilityBillingProvider for electricity.
///
/// Registered in DI when UtilityBills:Electricity:Enabled = false (the default).
/// Always returns a failure result with ErrorCode = "PROVIDER_NOT_CONFIGURED".
///
/// The job that receives this result skips scheduling a retry (since the failure
/// is due to configuration, not a transient network error) and does NOT increment
/// consecutive_failure_count.
///
/// Pattern mirrors NullEfawateercomGateway.cs.
/// </summary>
public sealed class NullElectricityBillingProvider : IUtilityBillingProvider
{
    public UtilityType ProviderType => UtilityType.Electricity;

    public Task<ProviderBillFetchResult> FetchBillsAsync(
        string accountNumber,
        string? meterNumber,
        DateOnly? sinceDate,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            ProviderBillFetchResult.Failure(
                errorCode: "PROVIDER_NOT_CONFIGURED",
                errorMessage: "Electricity billing provider is not enabled. " +
                              "Set UtilityBills:Electricity:Enabled = true and configure the provider."));
    }
}
