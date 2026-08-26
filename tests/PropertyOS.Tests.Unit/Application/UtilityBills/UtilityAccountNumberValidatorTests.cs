using FluentAssertions;
using PropertyOS.Application.UtilityBills.Commands.LinkUtilityAccount;
using PropertyOS.Application.UtilityBills.Commands.TenantLinkUtilityAccount;
using PropertyOS.Domain.UtilityBills.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.UtilityBills;

public sealed class UtilityAccountNumberValidatorTests
{
    [Theory]
    [InlineData("1234567890", true)]
    [InlineData("123456789", false)]
    [InlineData("12345678901", false)]
    [InlineData("12345A7890", false)]
    public void ManagementElectricity_RequiresExactlyTenDigits(string value, bool expected)
    {
        var result = new LinkUtilityAccountCommandValidator().Validate(
            new LinkUtilityAccountCommand(Guid.NewGuid(), UtilityType.Electricity, value));
        result.IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("12A", false)]
    [InlineData("12345678901234567890", true)]
    [InlineData("123456789012345678901", false)]
    public void TenantWater_RequiresDigitsWithinScraperContract(string value, bool expected)
    {
        var result = new TenantLinkUtilityAccountCommandValidator().Validate(
            new TenantLinkUtilityAccountCommand(UtilityType.Water, value));
        result.IsValid.Should().Be(expected);
    }
}
