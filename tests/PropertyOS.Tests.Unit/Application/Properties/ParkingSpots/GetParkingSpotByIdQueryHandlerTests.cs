using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.ParkingSpots.Queries.GetParkingSpotById;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.ParkingSpots;

public class GetParkingSpotByIdQueryHandlerTests
{
    private readonly IParkingSpotRepository _parkingSpotRepository = Substitute.For<IParkingSpotRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly GetParkingSpotByIdQueryHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();

    public GetParkingSpotByIdQueryHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _handler = new GetParkingSpotByIdQueryHandler(_parkingSpotRepository, _tenantContext);
    }

    [Fact]
    public async Task Handle_WithExistingSpot_ShouldReturnParkingSpotDto()
    {
        // Arrange
        var spotId = Guid.CreateVersion7();
        var spot = ParkingSpot.Create(CompanyId, BuildingId, "P1-01", DateTimeOffset.UtcNow, null, ParkingType.DisabledAccess, null, "Main Entrance");
        typeof(ParkingSpot).GetProperty("Id")?.SetValue(spot, spotId);

        _parkingSpotRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);

        var query = new GetParkingSpotByIdQuery(spotId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(spotId);
        result.SpotCode.Should().Be("P1-01");
        result.ParkingType.Should().Be(ParkingType.DisabledAccess);
        result.LocationDescription.Should().Be("Main Entrance");
        result.CompanyId.Should().Be(CompanyId);
        result.BuildingId.Should().Be(BuildingId);
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var query = new GetParkingSpotByIdQuery(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenSpotNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        _parkingSpotRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns((ParkingSpot?)null);

        var query = new GetParkingSpotByIdQuery(spotId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
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

        var query = new GetParkingSpotByIdQuery(spotId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenSpotArchived_ShouldThrowNotFound()
    {
        // Arrange
        var spotId = Guid.CreateVersion7();
        var spot = ParkingSpot.Create(CompanyId, BuildingId, "P1-01", DateTimeOffset.UtcNow, null);
        typeof(ParkingSpot).GetProperty("Id")?.SetValue(spot, spotId);
        spot.SoftDelete(DateTimeOffset.UtcNow, null);

        _parkingSpotRepository.GetByIdAsync(spotId, Arg.Any<CancellationToken>()).Returns(spot);

        var query = new GetParkingSpotByIdQuery(spotId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
