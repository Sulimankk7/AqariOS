using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Floors.Commands.CreateFloor;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Floors;

public class CreateFloorCommandHandlerTests
{
    private readonly IFloorRepository _floorRepository = Substitute.For<IFloorRepository>();
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly CreateFloorCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.NewGuid();

    public CreateFloorCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);

        _handler = new CreateFloorCommandHandler(
            _floorRepository,
            _buildingRepository,
            _tenantContext,
            _currentUserContext);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldAddFloor()
    {
        // Arrange
        var building = Building.Create(CompanyId, "Parent Building", 10, DateTimeOffset.UtcNow, UserId, id: BuildingId);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);
        _floorRepository.ExistsByFloorNumberAsync(BuildingId, 1, Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateFloorCommand(BuildingId, 1, "First Floor", FloorType.Regular);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _floorRepository.Received(1).AddAsync(Arg.Is<Floor>(f =>
            f.CompanyId == CompanyId &&
            f.BuildingId == BuildingId &&
            f.FloorNumber == 1 &&
            f.FloorLabel == "First Floor" &&
            f.FloorType == FloorType.Regular &&
            f.CreatedBy == UserId
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var command = new CreateFloorCommand(BuildingId, 1, "First Floor", FloorType.Regular);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingNotFound_ShouldThrowNotFound()
    {
        // Arrange
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns((Building?)null);
        var command = new CreateFloorCommand(BuildingId, 1, "First Floor", FloorType.Regular);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForDifferentCompanyBuilding_ShouldThrowNotFound()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var building = Building.Create(otherCompanyId, "Other Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);

        var command = new CreateFloorCommand(BuildingId, 1, "First Floor", FloorType.Regular);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingInactive_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var building = Building.Create(CompanyId, "Inactive Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);
        building.Deactivate(DateTimeOffset.UtcNow, null);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);

        var command = new CreateFloorCommand(BuildingId, 1, "First Floor", FloorType.Regular);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithDuplicateFloorNumber_ShouldThrowConflictException()
    {
        // Arrange
        var building = Building.Create(CompanyId, "Parent Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);
        _floorRepository.ExistsByFloorNumberAsync(BuildingId, 1, Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateFloorCommand(BuildingId, 1, "First Floor", FloorType.Regular);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
        ex.Message.Should().Contain("already exists");
    }
}
