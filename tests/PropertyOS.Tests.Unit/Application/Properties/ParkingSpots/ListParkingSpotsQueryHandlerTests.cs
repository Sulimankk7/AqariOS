using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.ParkingSpots.Queries.ListParkingSpots;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.ParkingSpots;

public class ListParkingSpotsQueryHandlerTests
{
    private readonly IParkingSpotRepository _parkingSpotRepository = Substitute.For<IParkingSpotRepository>();
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ListParkingSpotsQueryHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();

    public ListParkingSpotsQueryHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _handler = new ListParkingSpotsQueryHandler(_parkingSpotRepository, _buildingRepository, _tenantContext);
    }

    [Fact]
    public async Task Handle_WithValidBuilding_ShouldReturnMappedParkingSpots()
    {
        // Arrange
        var building = Building.Create(CompanyId, "Parent Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);

        var spot1 = ParkingSpot.Create(CompanyId, BuildingId, "P1-01", DateTimeOffset.UtcNow, null, ParkingType.Standard);
        var spot2 = ParkingSpot.Create(CompanyId, BuildingId, "P1-02", DateTimeOffset.UtcNow, null, ParkingType.Visitor);

        _parkingSpotRepository.ListByBuildingIdAsync(BuildingId, Arg.Any<CancellationToken>())
            .Returns(new List<ParkingSpot> { spot1, spot2 });

        var query = new ListParkingSpotsQuery(BuildingId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].SpotCode.Should().Be("P1-01");
        result[0].ParkingType.Should().Be(ParkingType.Standard);
        result[1].SpotCode.Should().Be("P1-02");
        result[1].ParkingType.Should().Be(ParkingType.Visitor);
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var query = new ListParkingSpotsQuery(BuildingId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingNotFound_ShouldThrowNotFound()
    {
        // Arrange
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns((Building?)null);
        var query = new ListParkingSpotsQuery(BuildingId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForDifferentCompanyBuilding_ShouldThrowNotFound()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var building = Building.Create(otherCompanyId, "Other Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);

        var query = new ListParkingSpotsQuery(BuildingId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingInactive_ShouldThrowNotFound()
    {
        // Arrange
        var building = Building.Create(CompanyId, "Inactive Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);
        building.Deactivate(DateTimeOffset.UtcNow, null);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);

        var query = new ListParkingSpotsQuery(BuildingId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
