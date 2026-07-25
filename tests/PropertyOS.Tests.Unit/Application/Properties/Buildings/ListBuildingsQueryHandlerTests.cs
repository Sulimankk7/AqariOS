using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Buildings.Queries.ListBuildings;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Buildings;

public class ListBuildingsQueryHandlerTests
{
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ListBuildingsQueryHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();

    public ListBuildingsQueryHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _handler = new ListBuildingsQueryHandler(_buildingRepository, _tenantContext);
    }

    [Fact]
    public async Task Handle_WithValidTenant_ShouldReturnMappedBuildings()
    {
        // Arrange
        var buildingId1 = Guid.CreateVersion7();
        var buildingId2 = Guid.CreateVersion7();
        var building1 = Building.Create(CompanyId, "Building Alpha", 5, DateTimeOffset.UtcNow, null, id: buildingId1);
        var building2 = Building.Create(CompanyId, "Building Beta", 12, DateTimeOffset.UtcNow, null, id: buildingId2);

        var address1 = BuildingAddress.Create(building1.Id, CompanyId, Governorate.Amman, "Amman", DateTimeOffset.UtcNow, "Khalda");
        building1.SetAddress(address1);

        _buildingRepository.ListByCompanyIdAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(new List<Building> { building1, building2 });

        var query = new ListBuildingsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Building Alpha");
        result[0].Address.Should().NotBeNull();
        result[0].Address!.District.Should().Be("Amman");
        result[1].Name.Should().Be("Building Beta");
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var query = new ListBuildingsQuery();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
