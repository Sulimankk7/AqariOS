using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.ParkingSpots.Commands.CreateParkingSpot;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.ParkingSpots;

public class CreateParkingSpotCommandHandlerTests
{
    private readonly IParkingSpotRepository _parkingSpotRepository = Substitute.For<IParkingSpotRepository>();
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly IApartmentRepository _apartmentRepository = Substitute.For<IApartmentRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly CreateParkingSpotCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.NewGuid();

    public CreateParkingSpotCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);

        _handler = new CreateParkingSpotCommandHandler(
            _parkingSpotRepository,
            _buildingRepository,
            _apartmentRepository,
            _tenantContext,
            _currentUserContext);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldAddParkingSpot()
    {
        // Arrange
        var building = Building.Create(CompanyId, "Parent Building", 10, DateTimeOffset.UtcNow, UserId, id: BuildingId);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);
        _parkingSpotRepository.ExistsBySpotCodeAsync(BuildingId, "P1-01", Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateParkingSpotCommand(BuildingId, "P1-01", ParkingType.Covered, null, "Underground B1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _parkingSpotRepository.Received(1).AddAsync(Arg.Is<ParkingSpot>(s =>
            s.CompanyId == CompanyId &&
            s.BuildingId == BuildingId &&
            s.SpotCode == "P1-01" &&
            s.ParkingType == ParkingType.Covered &&
            s.LocationDescription == "Underground B1" &&
            s.CreatedBy == UserId
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var command = new CreateParkingSpotCommand(BuildingId, "P1-01");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingNotFound_ShouldThrowNotFound()
    {
        // Arrange
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns((Building?)null);
        var command = new CreateParkingSpotCommand(BuildingId, "P1-01");

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

        var command = new CreateParkingSpotCommand(BuildingId, "P1-01");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingInactive_ShouldThrowNotFound()
    {
        // Arrange
        var building = Building.Create(CompanyId, "Inactive Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);
        building.Deactivate(DateTimeOffset.UtcNow, null);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);

        var command = new CreateParkingSpotCommand(BuildingId, "P1-01");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithDuplicateSpotCode_ShouldThrowConflictException()
    {
        // Arrange
        var building = Building.Create(CompanyId, "Parent Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);
        _parkingSpotRepository.ExistsBySpotCodeAsync(BuildingId, "P1-01", Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateParkingSpotCommand(BuildingId, "P1-01");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
        ex.Message.Should().Contain("already exists");
    }
}
