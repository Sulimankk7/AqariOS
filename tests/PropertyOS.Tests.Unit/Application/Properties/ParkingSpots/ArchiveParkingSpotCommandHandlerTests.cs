using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.ParkingSpots.Commands.ArchiveParkingSpot;
using PropertyOS.Application.Properties.ParkingSpots.Services;
using PropertyOS.Domain.Properties;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.ParkingSpots;

public class ArchiveParkingSpotCommandHandlerTests
{
    private readonly IParkingSpotRepository _parkingSpotRepository = Substitute.For<IParkingSpotRepository>();
    private readonly IParkingSpotArchiveDependencyChecker _dependencyChecker = Substitute.For<IParkingSpotArchiveDependencyChecker>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly Microsoft.Extensions.Logging.ILogger<ArchiveParkingSpotCommandHandler> _logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ArchiveParkingSpotCommandHandler>>();
    private readonly ArchiveParkingSpotCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.NewGuid();

    public ArchiveParkingSpotCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);

        _handler = new ArchiveParkingSpotCommandHandler(
            _parkingSpotRepository,
            _dependencyChecker,
            _tenantContext,
            _currentUserContext,
            _logger);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldSoftDeleteParkingSpot()
    {
        // Arrange
        var spotId = Guid.CreateVersion7();
        var spot = ParkingSpot.Create(CompanyId, BuildingId, "P1-01", DateTimeOffset.UtcNow, UserId);
        typeof(ParkingSpot).GetProperty("Id")?.SetValue(spot, spotId);

        _parkingSpotRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);

        var command = new ArchiveParkingSpotCommand(spotId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        spot.DeletedAt.Should().NotBeNull();
        spot.DeletedBy.Should().Be(UserId);
        spot.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var command = new ArchiveParkingSpotCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenSpotNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        _parkingSpotRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns((ParkingSpot?)null);

        var command = new ArchiveParkingSpotCommand(spotId);

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

        var command = new ArchiveParkingSpotCommand(spotId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyArchivedSpot_ShouldBeIdempotent()
    {
        // Arrange
        var spotId = Guid.CreateVersion7();
        var spot = ParkingSpot.Create(CompanyId, BuildingId, "P1-01", DateTimeOffset.UtcNow, UserId);
        typeof(ParkingSpot).GetProperty("Id")?.SetValue(spot, spotId);

        var firstDeletedAt = DateTimeOffset.UtcNow.AddHours(-1);
        spot.SoftDelete(firstDeletedAt, UserId);

        _parkingSpotRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);

        var command = new ArchiveParkingSpotCommand(spotId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        spot.DeletedAt.Should().Be(firstDeletedAt);
    }
}
