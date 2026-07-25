using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Floors.Commands.UpdateFloor;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Floors;

public class UpdateFloorCommandHandlerTests
{
    private readonly IFloorRepository _floorRepository = Substitute.For<IFloorRepository>();
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly UpdateFloorCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.NewGuid();

    public UpdateFloorCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);

        _handler = new UpdateFloorCommandHandler(
            _floorRepository,
            _buildingRepository,
            _tenantContext,
            _currentUserContext);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldUpdateFloorLabelAndType()
    {
        // Arrange
        var floorId = Guid.CreateVersion7();
        var floor = Floor.Create(CompanyId, BuildingId, 1, "Old Label", DateTimeOffset.UtcNow, UserId, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, floorId);

        var building = Building.Create(CompanyId, "Parent Building", 10, DateTimeOffset.UtcNow, UserId, id: BuildingId);

        _floorRepository.GetByIdAsync(floorId, Arg.Any<CancellationToken>()).Returns(floor);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);

        var command = new UpdateFloorCommand(floorId, "New Label", FloorType.Roof);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        floor.FloorLabel.Should().Be("New Label");
        floor.FloorType.Should().Be(FloorType.Roof);
        floor.FloorNumber.Should().Be(1); // Structural floor number remains unchanged
        floor.BuildingId.Should().Be(BuildingId); // Immutable parent building ID
        floor.CompanyId.Should().Be(CompanyId); // Immutable tenant company ID
        floor.UpdatedBy.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var command = new UpdateFloorCommand(Guid.NewGuid(), "New Label", FloorType.Regular);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenFloorNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var floorId = Guid.NewGuid();
        _floorRepository.GetByIdAsync(floorId, Arg.Any<CancellationToken>()).Returns((Floor?)null);

        var command = new UpdateFloorCommand(floorId, "New Label", FloorType.Regular);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForDifferentCompanyFloor_ShouldThrowNotFound()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var floorId = Guid.CreateVersion7();
        var floor = Floor.Create(otherCompanyId, BuildingId, 1, "Old Label", DateTimeOffset.UtcNow, null, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, floorId);

        _floorRepository.GetByIdAsync(floorId, Arg.Any<CancellationToken>()).Returns(floor);

        var command = new UpdateFloorCommand(floorId, "New Label", FloorType.Regular);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingInactive_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var floorId = Guid.CreateVersion7();
        var floor = Floor.Create(CompanyId, BuildingId, 1, "Old Label", DateTimeOffset.UtcNow, null, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, floorId);

        var building = Building.Create(CompanyId, "Inactive Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);
        building.Deactivate(DateTimeOffset.UtcNow, null);

        _floorRepository.GetByIdAsync(floorId, Arg.Any<CancellationToken>()).Returns(floor);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);

        var command = new UpdateFloorCommand(floorId, "New Label", FloorType.Regular);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
