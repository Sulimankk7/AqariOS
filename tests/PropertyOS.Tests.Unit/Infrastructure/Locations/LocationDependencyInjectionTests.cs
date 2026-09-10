using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Locations;
using PropertyOS.Infrastructure;
using PropertyOS.Infrastructure.Locations;

namespace PropertyOS.Tests.Unit.Infrastructure.Locations;

public sealed class LocationDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructureServices_RegistersConfiguredReverseGeocodingService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Database=propertyos_test;Username=test;Password=test",
                ["Geocoding:Provider"] = "Geoapify",
                ["Geocoding:BaseUrl"] = "https://api.geoapify.test/",
                ["Geocoding:ApiKey"] = "test-key",
                ["Geocoding:TimeoutSeconds"] = "7",
                ["Geocoding:CacheDurationSeconds"] = "60",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructureServices(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IReverseGeocodingService>()
            .Should().BeOfType<GeoapifyReverseGeocodingService>();
        var options = provider.GetRequiredService<IOptions<GeocodingOptions>>().Value;
        options.BaseUrl.Should().Be("https://api.geoapify.test/");
        options.TimeoutSeconds.Should().Be(7);
    }
}
