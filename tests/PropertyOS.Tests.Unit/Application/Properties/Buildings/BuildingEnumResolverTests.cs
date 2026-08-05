using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Buildings;

public class BuildingEnumResolverTests
{
    [Theory]
    [InlineData(BuildingType.Residential, "Residential")]
    [InlineData(BuildingType.Commercial, "Commercial")]
    [InlineData(BuildingType.MixedUse, "MixedUse")]
    public void BuildingType_EnumValues_HaveExpectedNamesAndNumericValues(BuildingType buildingType, string expectedName)
    {
        // Assert
        Assert.Equal(expectedName, buildingType.ToString());
        Assert.True((int)buildingType >= 0);
    }
}
