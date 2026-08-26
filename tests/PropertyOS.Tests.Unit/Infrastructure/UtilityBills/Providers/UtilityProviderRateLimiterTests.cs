using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Infrastructure.UtilityBills.Providers;
using Xunit;

namespace PropertyOS.Tests.Unit.Infrastructure.UtilityBills.Providers;

public class UtilityProviderRateLimiterTests
{
    private readonly IOptionsMonitor<UtilityBillsOptions> _optionsMonitor;
    private readonly ILogger<UtilityProviderRateLimiter> _logger;
    private readonly UtilityBillsOptions _options;

    public UtilityProviderRateLimiterTests()
    {
        _options = new UtilityBillsOptions
        {
            Electricity = new ElectricityOptions
            {
                MaxConcurrency = 2,
                RequestsPerMinute = 60
            },
            Water = new WaterOptions
            {
                MaxConcurrency = 1,
                RequestsPerMinute = 30
            }
        };

        _optionsMonitor = Substitute.For<IOptionsMonitor<UtilityBillsOptions>>();
        _optionsMonitor.CurrentValue.Returns(_options);
        _logger = Substitute.For<ILogger<UtilityProviderRateLimiter>>();
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_ExecutesAndReturnsResult()
    {
        using var limiter = new UtilityProviderRateLimiter(_optionsMonitor, _logger);

        var result = await limiter.ExecuteAsync(
            UtilityType.Electricity,
            ct => Task.FromResult(42),
            CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExceptionThrown_ReleasesSemaphoreForSubsequentCalls()
    {
        using var limiter = new UtilityProviderRateLimiter(_optionsMonitor, _logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            limiter.ExecuteAsync<int>(
                UtilityType.Electricity,
                ct => throw new InvalidOperationException("Provider failed"),
                CancellationToken.None));

        // Subsequent call must succeed because semaphore was released in finally block
        var nextResult = await limiter.ExecuteAsync(
            UtilityType.Electricity,
            ct => Task.FromResult(100),
            CancellationToken.None);

        Assert.Equal(100, nextResult);
    }

    [Fact]
    public async Task ExecuteAsync_IndependentLimits_ElectricityAndWaterDoNotBlockEachOther()
    {
        using var limiter = new UtilityProviderRateLimiter(_optionsMonitor, _logger);

        var t1 = limiter.ExecuteAsync(UtilityType.Electricity, async ct =>
        {
            await Task.Delay(10, ct);
            return "ELEC";
        }, CancellationToken.None);

        var t2 = limiter.ExecuteAsync(UtilityType.Water, async ct =>
        {
            await Task.Delay(10, ct);
            return "WATER";
        }, CancellationToken.None);

        await Task.WhenAll(t1, t2);

        Assert.Equal("ELEC", await t1);
        Assert.Equal("WATER", await t2);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var limiter = new UtilityProviderRateLimiter(_optionsMonitor, _logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            limiter.ExecuteAsync(
                UtilityType.Electricity,
                ct => Task.FromResult(1),
                cts.Token));
    }
}

