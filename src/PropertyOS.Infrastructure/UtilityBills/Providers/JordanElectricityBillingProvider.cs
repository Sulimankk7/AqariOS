using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Infrastructure.UtilityBills.Providers;

/// <summary>Thin AqariOS adapter for the internal IDECO Python scraper endpoint.</summary>
public sealed class JordanElectricityBillingProvider : IUtilityBillingProvider
{
    private readonly InternalUtilityScraperClient _client;
    private readonly IOptionsSnapshot<UtilityBillsOptions> _options;
    private readonly UtilityProviderRateLimiter _rateLimiter;
    private readonly ILogger<JordanElectricityBillingProvider> _logger;

    public JordanElectricityBillingProvider(
        InternalUtilityScraperClient client,
        IOptionsSnapshot<UtilityBillsOptions> options,
        UtilityProviderRateLimiter rateLimiter,
        ILogger<JordanElectricityBillingProvider> logger)
    {
        _client = client;
        _options = options;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public UtilityType ProviderType => UtilityType.Electricity;

    public Task<ProviderBillFetchResult> FetchBillsAsync(
        string accountNumber,
        string? meterNumber,
        DateOnly? sinceDate,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Value.Electricity.Enabled || !_client.IsConfigured)
        {
            _logger.LogInformation("Electricity scraper integration is disabled or unconfigured.");
            return Task.FromResult(ProviderBillFetchResult.Failure(
                "PROVIDER_NOT_CONFIGURED",
                "Electricity provider integration is not configured or disabled."));
        }

        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return Task.FromResult(ProviderBillFetchResult.Failure(
                "INVALID_ACCOUNT", "An electricity account number is required."));
        }

        return _rateLimiter.ExecuteAsync(
            UtilityType.Electricity,
            ct => _client.FetchBillsAsync(UtilityType.Electricity, accountNumber, sinceDate, ct),
            cancellationToken);
    }
}
