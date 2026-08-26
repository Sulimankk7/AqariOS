using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Infrastructure.UtilityBills.Providers;

/// <summary>Thin AqariOS adapter for the internal YW Python scraper endpoint.</summary>
public sealed class JordanWaterBillingProvider : IUtilityBillingProvider
{
    private readonly InternalUtilityScraperClient _client;
    private readonly IOptionsSnapshot<UtilityBillsOptions> _options;
    private readonly UtilityProviderRateLimiter _rateLimiter;
    private readonly ILogger<JordanWaterBillingProvider> _logger;

    public JordanWaterBillingProvider(
        InternalUtilityScraperClient client,
        IOptionsSnapshot<UtilityBillsOptions> options,
        UtilityProviderRateLimiter rateLimiter,
        ILogger<JordanWaterBillingProvider> logger)
    {
        _client = client;
        _options = options;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public UtilityType ProviderType => UtilityType.Water;

    public Task<ProviderBillFetchResult> FetchBillsAsync(
        string accountNumber,
        string? meterNumber,
        DateOnly? sinceDate,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Value.Water.Enabled || !_client.IsConfigured)
        {
            _logger.LogInformation("Water scraper integration is disabled or unconfigured.");
            return Task.FromResult(ProviderBillFetchResult.Failure(
                "PROVIDER_NOT_CONFIGURED",
                "Water provider integration is not configured or disabled."));
        }

        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return Task.FromResult(ProviderBillFetchResult.Failure(
                "INVALID_ACCOUNT", "A water subscription number is required."));
        }

        return _rateLimiter.ExecuteAsync(
            UtilityType.Water,
            ct => _client.FetchBillsAsync(UtilityType.Water, accountNumber, sinceDate, ct),
            cancellationToken);
    }
}
