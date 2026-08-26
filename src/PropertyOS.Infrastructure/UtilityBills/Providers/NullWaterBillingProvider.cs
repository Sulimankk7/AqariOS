using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Infrastructure.UtilityBills.Providers;

/// <summary>
/// Fail-closed no-op implementation of IUtilityBillingProvider for water.
///
/// Registered in DI when UtilityBills:Water:Enabled = false (the default).
/// Always returns a failure result with ErrorCode = "PROVIDER_NOT_CONFIGURED".
///
/// Pattern mirrors NullEfawateercomGateway.cs.
/// </summary>
public sealed class NullWaterBillingProvider : IUtilityBillingProvider
{
    public UtilityType ProviderType => UtilityType.Water;

    public Task<ProviderBillFetchResult> FetchBillsAsync(
        string accountNumber,
        string? meterNumber,
        DateOnly? sinceDate,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            ProviderBillFetchResult.Failure(
                errorCode: "PROVIDER_NOT_CONFIGURED",
                errorMessage: "Water billing provider is not enabled. " +
                              "Set UtilityBills:Water:Enabled = true and configure the provider."));
    }
}
