using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Locations;
using PropertyOS.Infrastructure.Locations;

namespace PropertyOS.Tests.Unit.Infrastructure.Locations;

public sealed class GeoapifyReverseGeocodingServiceTests
{
    [Fact]
    public async Task MapsArabicResponseWithoutLeakingProviderPayload()
    {
        var service = Create(HttpStatusCode.OK, """{"results":[{"country_code":"jo","country":"الأردن","state":"عمّان","city":"عمّان","suburb":"تلاع العلي","street":"شارع وصفي التل"}]}""", out var handler);
        var result = await service.ReverseAsync(31.963158m, 35.930359m, "ar");
        result.Should().NotBeNull();
        result!.CountryCode.Should().Be("JO");
        result.Neighborhood.Should().Be("تلاع العلي");
        result.PostalCode.Should().BeNull();
        handler.LastUri!.Query.Should().Contain("lang=ar").And.Contain("apiKey=secret-test-key");
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ProviderFailureReturnsNoEnrichment(HttpStatusCode status)
    {
        var service = Create(status, "provider secret body", out _);
        (await service.ReverseAsync(31m, 35m, "en")).Should().BeNull();
    }

    [Fact]
    public async Task MalformedResponseReturnsNoEnrichment()
    {
        var service = Create(HttpStatusCode.OK, "not-json", out _);
        (await service.ReverseAsync(31m, 35m, "en")).Should().BeNull();
    }

    [Fact]
    public async Task InvalidCoordinatesAreRejected()
    {
        var service = Create(HttpStatusCode.OK, "{}", out _);
        await FluentActions.Invoking(() => service.ReverseAsync(91m, 35m, "en")).Should().ThrowAsync<ArgumentOutOfRangeException>();
        await FluentActions.Invoking(() => service.ReverseAsync(31m, 181m, "en")).Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    private static GeoapifyReverseGeocodingService Create(HttpStatusCode status, string body, out Handler handler)
    {
        handler = new Handler(status, body);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.geoapify.com/") };
        var options = Options.Create(new GeocodingOptions { Provider = "Geoapify", ApiKey = "secret-test-key", CacheDurationSeconds = 60 });
        return new(client, options, new MemoryCache(new MemoryCacheOptions()), NullLogger<GeoapifyReverseGeocodingService>.Instance);
    }

    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
