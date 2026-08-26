using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Infrastructure.UtilityBills.Providers;

namespace PropertyOS.Tests.Unit.Infrastructure.UtilityBills.Providers;

public class JordanElectricityBillingProviderTests
{
    private const string SharedSecret = "unit-test-secret-with-at-least-32-bytes";

    [Fact]
    public async Task FetchBillsAsync_WhenDisabled_FailsClosedWithoutHttpCall()
    {
        var fixture = new ProviderFixture();
        fixture.Options.Electricity.Enabled = false;

        var result = await fixture.Provider.FetchBillsAsync("0123456789", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROVIDER_NOT_CONFIGURED", result.ErrorCode);
        Assert.Equal(0, fixture.Handler.RequestCount);
    }

    [Fact]
    public async Task FetchBillsAsync_WhenScraperConfigurationMissing_FailsClosed()
    {
        var fixture = new ProviderFixture();
        fixture.Options.ScraperService.SharedSecret = "";

        var result = await fixture.Provider.FetchBillsAsync("0123456789", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROVIDER_NOT_CONFIGURED", result.ErrorCode);
        Assert.Equal(0, fixture.Handler.RequestCount);
    }

    [Fact]
    public async Task FetchBillsAsync_SendsOnlyAccountNumberWithValidHmacAndMapsNormalizedBill()
    {
        var fixture = new ProviderFixture();
        HttpRequestMessage? capturedRequest = null;
        byte[]? capturedBody = null;
        fixture.Handler.Handler = async (request, _) =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsByteArrayAsync();
            return JsonResponse("""
                {
                  "success": true,
                  "total_outstanding_balance": 20.000,
                  "bills": [{
                    "external_id": "ideco:abc",
                    "bill_date": "2026-05-01",
                    "due_date": null,
                    "amount": 54.750,
                    "amount_due": 20.000,
                    "paid_amount": 34.750,
                    "remaining_amount": 20.000,
                    "status": "UNPAID",
                    "consumption": 318,
                    "reference": "IssuYM=202605&CityId=26&CusmId=42",
                    "currency": "JOD"
                  }]
                }
                """);
        };

        var result = await fixture.Provider.FetchBillsAsync("0123456789", "ignored-meter", null);

        Assert.True(result.IsSuccess);
        var bill = Assert.Single(result.Bills);
        Assert.Equal("ideco:abc", bill.ExternalId);
        Assert.Equal(new DateOnly(2026, 5, 1), bill.BillDate);
        Assert.Equal(54.750m, bill.Amount);
        Assert.False(bill.IsPaid);
        Assert.Equal(UtilityBillPaymentStatus.Unpaid, bill.PaymentStatus);
        Assert.Equal(20.000m, result.TotalOutstandingBalance);

        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedBody);
        Assert.Equal("/internal/v1/electricity/bills", capturedRequest.RequestUri!.AbsolutePath);
        using var document = JsonDocument.Parse(capturedBody);
        var properties = document.RootElement.EnumerateObject().ToList();
        var property = Assert.Single(properties);
        Assert.Equal("account_number", property.Name);
        Assert.Equal("0123456789", property.Value.GetString());

        var timestamp = Assert.Single(capturedRequest.Headers.GetValues("X-AqariOS-Timestamp"));
        var nonce = Assert.Single(capturedRequest.Headers.GetValues("X-AqariOS-Nonce"));
        var signature = Assert.Single(capturedRequest.Headers.GetValues("X-AqariOS-Signature"));
        var bodyHash = Convert.ToHexStringLower(SHA256.HashData(capturedBody));
        var canonical = Encoding.UTF8.GetBytes(
            $"{timestamp}\n{nonce}\nPOST\n/internal/v1/electricity/bills\n{bodyHash}");
        var expected = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(SharedSecret), canonical));
        Assert.Equal(expected, signature);
    }

    [Fact]
    public async Task FetchBillsAsync_FiltersRecordsOlderThanSinceDate()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(JsonResponse("""
            {
              "success": true,
              "bills": [
                {"external_id":"old","bill_date":"2026-03-01","amount":20,"status":"PAID","currency":"JOD"},
                {"external_id":"new","bill_date":"2026-05-01","amount":35,"status":"UNPAID","currency":"JOD"}
              ]
            }
            """));

        var result = await fixture.Provider.FetchBillsAsync(
            "0123456789", null, new DateOnly(2026, 4, 1));

        Assert.True(result.IsSuccess);
        Assert.Equal("new", Assert.Single(result.Bills).ExternalId);
        Assert.Null(result.TotalOutstandingBalance);
    }

    [Fact]
    public async Task FetchBillsAsync_FullHistoricalResponsePreservesAllPaidAndUnpaidRecords()
    {
        var fixture = new ProviderFixture();
        var responseBody = JsonSerializer.Serialize(new
        {
            success = true,
            bills = Enumerable.Range(1, 8).Select(month => new
            {
                external_id = $"ideco:history:{month:00}",
                bill_date = $"2026-{month:00}-01",
                payment_date = month <= 5 ? $"2026-{month:00}-15" : null,
                amount = 20m + month,
                status = month <= 5 ? "PAID" : "UNPAID",
                currency = "JOD",
                reference = $"fixture-{month:00}"
            })
        });
        fixture.Handler.Handler = (_, _) => Task.FromResult(JsonResponse(responseBody));

        var result = await fixture.Provider.FetchBillsAsync("0123456789", null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(8, result.Bills.Count);
        Assert.Equal(5, result.Bills.Count(bill => bill.IsPaid));
        Assert.Equal(3, result.Bills.Count(bill => !bill.IsPaid));
        Assert.Equal(8, result.Bills.Select(bill => bill.ExternalId).Distinct().Count());
    }

    [Fact]
    public async Task FetchBillsAsync_ControlledServiceErrorPreservesSafeErrorCode()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(JsonResponse(
            """{"success":false,"bills":[],"error_code":"PARSING_ERROR","error_message":"Provider response changed."}""",
            HttpStatusCode.BadGateway));

        var result = await fixture.Provider.FetchBillsAsync("0123456789", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("PARSING_ERROR", result.ErrorCode);
        Assert.Equal("Provider response changed.", result.ErrorMessage);
    }

    [Fact]
    public async Task FetchBillsAsync_MalformedResponseFailsClosed()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(JsonResponse("not-json"));

        var result = await fixture.Provider.FetchBillsAsync("0123456789", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("PARSING_ERROR", result.ErrorCode);
    }

    [Fact]
    public async Task FetchBillsAsync_EmptyAccountNumberReturnsInvalidAccount()
    {
        var fixture = new ProviderFixture();

        var result = await fixture.Provider.FetchBillsAsync("   ", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_ACCOUNT", result.ErrorCode);
        Assert.Equal(0, fixture.Handler.RequestCount);
    }

    [Fact]
    public async Task FetchBillsAsync_UnauthorizedServiceResponseReturnsAuthFailure()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var result = await fixture.Provider.FetchBillsAsync("0123456789", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("AUTH_FAILURE", result.ErrorCode);
    }

    [Fact]
    public async Task FetchBillsAsync_ControlledInvalidAccountResponseIsPreserved()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(JsonResponse(
            """{"success":false,"bills":[],"error_code":"INVALID_ACCOUNT","error_message":"Invalid account."}""",
            HttpStatusCode.UnprocessableEntity));

        var result = await fixture.Provider.FetchBillsAsync("0123456789", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_ACCOUNT", result.ErrorCode);
    }

    [Fact]
    public async Task FetchBillsAsync_TooManyRequestsReturnsRateLimited()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        var result = await fixture.Provider.FetchBillsAsync("0123456789", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("RATE_LIMITED", result.ErrorCode);
    }

    [Fact]
    public async Task FetchBillsAsync_UncontrolledServerFailureReturnsProviderUnavailable()
    {
        var fixture = new ProviderFixture();
        fixture.Handler.Handler = (_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await fixture.Provider.FetchBillsAsync("0123456789", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROVIDER_UNAVAILABLE", result.ErrorCode);
    }

    private static HttpResponseMessage JsonResponse(
        string json, HttpStatusCode statusCode = HttpStatusCode.OK) => new(statusCode)
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
                SharedSecret = SharedSecret
            },
            Electricity = new ElectricityOptions
            {
                Enabled = true,
                RequestsPerMinute = 60,
                MaxConcurrency = 2
            }
        };

        public FakeHttpMessageHandler Handler { get; } = new();
        public JordanElectricityBillingProvider Provider { get; }

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
            Provider = new JordanElectricityBillingProvider(
                client,
                snapshot,
                rateLimiter,
                Substitute.For<ILogger<JordanElectricityBillingProvider>>());
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
