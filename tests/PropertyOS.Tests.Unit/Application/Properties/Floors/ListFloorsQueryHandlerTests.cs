using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Floors.Queries.ListFloors;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Floors;

public class ListFloorsQueryHandlerTests
{
    private readonly IFloorRepository _floorRepository = Substitute.For<IFloorRepository>();
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ListFloorsQueryHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();

    public ListFloorsQueryHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _handler = new ListFloorsQueryHandler(_floorRepository, _buildingRepository, _tenantContext);
    }

    [Fact]
    public async Task Handle_WithValidBuilding_ShouldReturnMappedFloors()
    {
        // Arrange
        var building = Building.Create(CompanyId, "Parent Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);

        var floor1 = Floor.Create(CompanyId, BuildingId, 0, "Ground Floor", DateTimeOffset.UtcNow, null, FloorType.Ground);
        var floor2 = Floor.Create(CompanyId, BuildingId, 1, "First Floor", DateTimeOffset.UtcNow, null, FloorType.Regular);

        _floorRepository.ListByBuildingIdAsync(BuildingId, Arg.Any<CancellationToken>())
            .Returns(new List<Floor> { floor1, floor2 });

        var query = new ListFloorsQuery(BuildingId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].FloorNumber.Should().Be(0);
        result[0].FloorLabel.Should().Be("Ground Floor");
        result[1].FloorNumber.Should().Be(1);
        result[1].FloorLabel.Should().Be("First Floor");
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var query = new ListFloorsQuery(BuildingId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingNotFound_ShouldThrowNotFound()
    {
        // Arrange
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns((Building?)null);
        var query = new ListFloorsQuery(BuildingId);

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

        var query = new ListFloorsQuery(BuildingId);

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

        var query = new ListFloorsQuery(BuildingId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
