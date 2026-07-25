using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.ParkingSpots.Commands.UpdateParkingSpot;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.ParkingSpots;

public class UpdateParkingSpotCommandHandlerTests
{
    private readonly IParkingSpotRepository _parkingSpotRepository = Substitute.For<IParkingSpotRepository>();
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly IApartmentRepository _apartmentRepository = Substitute.For<IApartmentRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly UpdateParkingSpotCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.NewGuid();

    public UpdateParkingSpotCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);

        _handler = new UpdateParkingSpotCommandHandler(
            _parkingSpotRepository,
            _buildingRepository,
            _apartmentRepository,
            _tenantContext,
            _currentUserContext);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldUpdateParkingSpotDetails()
    {
        // Arrange
        var spotId = Guid.CreateVersion7();
        var spot = ParkingSpot.Create(CompanyId, BuildingId, "P1-01", DateTimeOffset.UtcNow, UserId, ParkingType.Standard);
        typeof(ParkingSpot).GetProperty("Id")?.SetValue(spot, spotId);

        var building = Building.Create(CompanyId, "Parent Building", 10, DateTimeOffset.UtcNow, UserId, id: BuildingId);

        _parkingSpotRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);

        var command = new UpdateParkingSpotCommand(spotId, "P1-01-NEW", ParkingType.DisabledAccess, null, "Updated Level B2");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        spot.SpotCode.Should().Be("P1-01-NEW");
        spot.ParkingType.Should().Be(ParkingType.DisabledAccess);
        spot.LocationDescription.Should().Be("Updated Level B2");
        spot.BuildingId.Should().Be(BuildingId);
        spot.CompanyId.Should().Be(CompanyId);
        spot.UpdatedBy.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var command = new UpdateParkingSpotCommand(Guid.NewGuid(), "P1-01", ParkingType.Standard);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenSpotNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        _parkingSpotRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns((ParkingSpot?)null);

        var command = new UpdateParkingSpotCommand(spotId, "P1-01", ParkingType.Standard);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForDifferentCompanySpot_ShouldThrowNotFound()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var spotId = Guid.CreateVersion7();
        var spot = ParkingSpot.Create(otherCompanyId, BuildingId, "P1-01", DateTimeOffset.UtcNow, null);
        typeof(ParkingSpot).GetProperty("Id")?.SetValue(spot, spotId);

        _parkingSpotRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);

        var command = new UpdateParkingSpotCommand(spotId, "P1-01", ParkingType.Standard);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithDuplicateSpotCode_ShouldThrowConflictException()
    {
        // Arrange
        var spotId = Guid.CreateVersion7();
        var spot = ParkingSpot.Create(CompanyId, BuildingId, "P1-01", DateTimeOffset.UtcNow, null);
        typeof(ParkingSpot).GetProperty("Id")?.SetValue(spot, spotId);

        var building = Building.Create(CompanyId, "Parent Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);

        _parkingSpotRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);
        _parkingSpotRepository.ExistsBySpotCodeAsync(BuildingId, "P1-DUP", Arg.Any<CancellationToken>()).Returns(true);

        var command = new UpdateParkingSpotCommand(spotId, "P1-DUP", ParkingType.Standard);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
