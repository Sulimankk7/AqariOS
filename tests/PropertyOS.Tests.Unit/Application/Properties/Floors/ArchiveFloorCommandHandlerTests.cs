using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Floors.Commands.ArchiveFloor;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Floors;

public class ArchiveFloorCommandHandlerTests
{
    private readonly IFloorRepository _floorRepository = Substitute.For<IFloorRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly ArchiveFloorCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.NewGuid();

    public ArchiveFloorCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);

        _handler = new ArchiveFloorCommandHandler(
            _floorRepository,
            _tenantContext,
            _currentUserContext);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldSoftDeleteFloor()
    {
        // Arrange
        var floorId = Guid.CreateVersion7();
        var floor = Floor.Create(CompanyId, BuildingId, 1, "Target Floor", DateTimeOffset.UtcNow, UserId, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, floorId);

        _floorRepository.GetByIdAsync(floorId, Arg.Any<CancellationToken>()).Returns(floor);

        var command = new ArchiveFloorCommand(floorId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        floor.DeletedAt.Should().NotBeNull();
        floor.DeletedBy.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var command = new ArchiveFloorCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenFloorNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var floorId = Guid.NewGuid();
        _floorRepository.GetByIdAsync(floorId, Arg.Any<CancellationToken>()).Returns((Floor?)null);

        var command = new ArchiveFloorCommand(floorId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForDifferentCompanyFloor_ShouldThrowNotFound()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var floorId = Guid.CreateVersion7();
        var floor = Floor.Create(otherCompanyId, BuildingId, 1, "Other Floor", DateTimeOffset.UtcNow, null, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, floorId);

        _floorRepository.GetByIdAsync(floorId, Arg.Any<CancellationToken>()).Returns(floor);

        var command = new ArchiveFloorCommand(floorId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyArchivedFloor_ShouldBeIdempotent()
    {
        // Arrange
        var floorId = Guid.CreateVersion7();
        var floor = Floor.Create(CompanyId, BuildingId, 1, "Target Floor", DateTimeOffset.UtcNow, UserId, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, floorId);

        var firstDeletedAt = DateTimeOffset.UtcNow.AddHours(-2);
        floor.SoftDelete(firstDeletedAt, UserId);

        _floorRepository.GetByIdAsync(floorId, Arg.Any<CancellationToken>()).Returns(floor);

        var command = new ArchiveFloorCommand(floorId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        floor.DeletedAt.Should().Be(firstDeletedAt);
    }
}
