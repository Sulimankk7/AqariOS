using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Leasing;

public class TenantPhoneNumberTests
{
    [Theory]
    [InlineData("+962798425056", null, "+962798425056")]
    [InlineData("+962 79 842 5056", null, "+962798425056")]
    [InlineData("0798425056", "JO", "+962798425056")]
    [InlineData("+966 55 123 4567", null, "+966551234567")]
    [InlineData("+971-50-123-4567", null, "+971501234567")]
    [InlineData("+91 (98765) 43210", null, "+919876543210")]
    public void Normalize_ReturnsCanonicalE164(string input, string? countryCode, string expected)
    {
        Assert.Equal(expected, TenantPhoneNumber.Normalize(input, countryCode));
    }

    [Fact]
    public void Normalize_LocalJordanianNumberUsesTenantDefaultCountry()
    {
        Assert.Equal("+962798425056", TenantPhoneNumber.Normalize("0798425056"));
    }

    [Fact]
    public void TenantAggregate_CanonicalizesCreateAndUpdateWrites()
    {
        var tenant = Tenant.Create(
            Guid.NewGuid(), "Tenant", "N-1", "0798425056", DateTimeOffset.UtcNow, null,
            phoneCountryCode: "JO");

        Assert.Equal("+962798425056", tenant.Phone);

        tenant.UpdateDetails(
            "Tenant", "N-1", "+971 50 123 4567", null, null,
            DateTimeOffset.UtcNow, null);

        Assert.Equal("+971501234567", tenant.Phone);
    }
}
