using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Infrastructure.UtilityBills.Providers;

namespace PropertyOS.Tests.Unit.Infrastructure.UtilityBills.Providers;

public class JordanWaterBillingProviderTests
{
    [Fact]
    public async Task FetchBillsAsync_WhenDisabled_FailsClosed()
    {
        var fixture = new ProviderFixture();
        fixture.Options.Water.Enabled = false;

        var result = await fixture.Provider.FetchBillsAsync("96309", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROVIDER_NOT_CONFIGURED", result.ErrorCode);
        Assert.Equal(0, fixture.Handler.RequestCount);
    }

    [Fact]
    public async Task FetchBillsAsync_BootstrapMapsPaidAndUnpaidHistory()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(JsonResponse("""
            {
              "success": true,
              "total_outstanding_balance": 19.50,
              "bills": [
                {"external_id":"water:1","bill_date":"2026-01-20","amount":18.25,"status":"PAID","currency":"JOD"},
                {"external_id":"water:2","bill_date":"2026-02-18","amount":21.00,"status":"PAID","currency":"JOD"},
                {"external_id":"water:3","bill_date":"2026-03-17","amount":19.50,"status":"UNPAID","currency":"JOD"}
              ]
            }
            """));

        var result = await fixture.Provider.FetchBillsAsync("96309", null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Bills.Count);
        Assert.Equal(UtilityBillPaymentStatus.Paid, result.Bills[0].PaymentStatus);
        Assert.Equal(UtilityBillPaymentStatus.Paid, result.Bills[1].PaymentStatus);
        Assert.Equal(UtilityBillPaymentStatus.Unpaid, result.Bills[2].PaymentStatus);
        Assert.Equal(19.50m, result.Bills[2].Amount);
        Assert.Equal(19.50m, result.TotalOutstandingBalance);
    }

    [Fact]
    public async Task FetchBillsAsync_IncrementalModeFiltersOlderBills()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(JsonResponse("""
            {
              "success": true,
              "bills": [
                {"external_id":"old","bill_date":"2026-02-18","amount":20,"status":"PAID","currency":"JOD"},
                {"external_id":"new","bill_date":"2026-03-17","amount":22,"status":"UNPAID","currency":"JOD"}
              ]
            }
            """));

        var result = await fixture.Provider.FetchBillsAsync(
            "96309", null, new DateOnly(2026, 3, 1));

        Assert.True(result.IsSuccess);
        Assert.Equal("new", Assert.Single(result.Bills).ExternalId);
    }

    [Fact]
    public async Task FetchBillsAsync_UnauthorizedServiceResponseFailsClosed()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var result = await fixture.Provider.FetchBillsAsync("96309", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("AUTH_FAILURE", result.ErrorCode);
    }

    [Fact]
    public async Task FetchBillsAsync_ControlledInvalidAccountResponseIsPreserved()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(new HttpResponseMessage(
            HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent(
                """{"success":false,"bills":[],"error_code":"INVALID_ACCOUNT","error_message":"Invalid subscription."}""",
                Encoding.UTF8,
                "application/json")
        });

        var result = await fixture.Provider.FetchBillsAsync("96309", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_ACCOUNT", result.ErrorCode);
    }

    [Fact]
    public async Task FetchBillsAsync_TooManyRequestsReturnsRateLimited()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        var result = await fixture.Provider.FetchBillsAsync("96309", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("RATE_LIMITED", result.ErrorCode);
    }

    [Fact]
    public async Task FetchBillsAsync_UncontrolledServerFailureReturnsProviderUnavailable()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await fixture.Provider.FetchBillsAsync("96309", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROVIDER_UNAVAILABLE", result.ErrorCode);
    }

    [Fact]
    public async Task FetchBillsAsync_MalformedResponseFailsClosed()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(JsonResponse("not-json"));

        var result = await fixture.Provider.FetchBillsAsync("96309", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("PARSING_ERROR", result.ErrorCode);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class ProviderFixture
    {
        public UtilityBillsOptions Options { get; } = new()
        {
            ScraperService = new UtilityScraperServiceOptions
            {
                BaseUrl = "https://utility-scraper.internal",
                SharedSecret = "unit-test-secret-with-at-least-32-bytes"
            },
            Water = new WaterOptions
            {
                Enabled = true,
                RequestsPerMinute = 60,
                MaxConcurrency = 1
            }
        };

        public FakeHttpMessageHandler Handler { get; } = new();
        public JordanWaterBillingProvider Provider { get; }

        public ProviderFixture()
        {
            var snapshot = Substitute.For<IOptionsSnapshot<UtilityBillsOptions>>();
            snapshot.Value.Returns(Options);
            var monitor = Substitute.For<IOptionsMonitor<UtilityBillsOptions>>();
            monitor.CurrentValue.Returns(Options);
            var rateLimiter = new UtilityProviderRateLimiter(
                monitor, Substitute.For<ILogger<UtilityProviderRateLimiter>>());
            var client = new InternalUtilityScraperClient(
                new HttpClient(Handler),
                snapshot,
                Substitute.For<ILogger<InternalUtilityScraperClient>>());
            Provider = new JordanWaterBillingProvider(
                client,
                snapshot,
                rateLimiter,
                Substitute.For<ILogger<JordanWaterBillingProvider>>());
        }
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Handler { get; set; }
            = (_, _) => Task.FromResult(JsonResponse("{\"success\":true,\"bills\":[]}"));

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Handler(request, cancellationToken);
        }
    }
}
