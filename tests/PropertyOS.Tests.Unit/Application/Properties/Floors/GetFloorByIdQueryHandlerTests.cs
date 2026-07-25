using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Floors.Queries.GetFloorById;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Floors;

public class GetFloorByIdQueryHandlerTests
{
    private readonly IFloorRepository _floorRepository = Substitute.For<IFloorRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly GetFloorByIdQueryHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();

    public GetFloorByIdQueryHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _handler = new GetFloorByIdQueryHandler(_floorRepository, _tenantContext);
    }

    [Fact]
    public async Task Handle_WithExistingFloor_ShouldReturnFloorDto()
    {
        // Arrange
        var floorId = Guid.CreateVersion7();
        var floor = Floor.Create(CompanyId, BuildingId, 1, "First Floor", DateTimeOffset.UtcNow, null, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, floorId);

        _floorRepository.GetByIdAsync(floorId, Arg.Any<CancellationToken>()).Returns(floor);

        var query = new GetFloorByIdQuery(floorId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(floorId);
        result.FloorLabel.Should().Be("First Floor");
        result.FloorNumber.Should().Be(1);
        result.CompanyId.Should().Be(CompanyId);
        result.BuildingId.Should().Be(BuildingId);
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var query = new GetFloorByIdQuery(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenFloorNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var floorId = Guid.NewGuid();
        _floorRepository.GetByIdAsync(floorId, Arg.Any<CancellationToken>()).Returns((Floor?)null);

        var query = new GetFloorByIdQuery(floorId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForDifferentCompanyFloor_ShouldThrowNotFound()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var floorId = Guid.CreateVersion7();
        var floor = Floor.Create(otherCompanyId, BuildingId, 1, "First Floor", DateTimeOffset.UtcNow, null, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, floorId);

        _floorRepository.GetByIdAsync(floorId, Arg.Any<CancellationToken>()).Returns(floor);

        var query = new GetFloorByIdQuery(floorId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenFloorArchived_ShouldThrowNotFound()
    {
        // Arrange
        var floorId = Guid.CreateVersion7();
        var floor = Floor.Create(CompanyId, BuildingId, 1, "First Floor", DateTimeOffset.UtcNow, null, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, floorId);
        floor.SoftDelete(DateTimeOffset.UtcNow, null);

        _floorRepository.GetByIdAsync(floorId, Arg.Any<CancellationToken>()).Returns(floor);

        var query = new GetFloorByIdQuery(floorId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
