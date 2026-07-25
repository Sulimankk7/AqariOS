using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Buildings.Commands.ArchiveBuilding;
using PropertyOS.Domain.Properties;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Buildings;

public class ArchiveBuildingCommandHandlerTests
{
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly ArchiveBuildingCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public ArchiveBuildingCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);

        _handler = new ArchiveBuildingCommandHandler(
            _buildingRepository,
            _tenantContext,
            _currentUserContext);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldSoftDeleteBuilding()
    {
        // Arrange
        var buildingId = Guid.CreateVersion7();
        var building = Building.Create(CompanyId, "Target Building", 10, DateTimeOffset.UtcNow, UserId, id: buildingId);
        _buildingRepository.GetByIdAsync(buildingId, Arg.Any<CancellationToken>()).Returns(building);

        var command = new ArchiveBuildingCommand(buildingId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        building.DeletedAt.Should().NotBeNull();
        building.DeletedBy.Should().Be(UserId);
        building.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var command = new ArchiveBuildingCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        _buildingRepository.GetByIdAsync(buildingId, Arg.Any<CancellationToken>()).Returns((Building?)null);

        var command = new ArchiveBuildingCommand(buildingId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForDifferentCompany_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var buildingId = Guid.CreateVersion7();
        var building = Building.Create(otherCompanyId, "Other Building", 10, DateTimeOffset.UtcNow, null, id: buildingId);

        _buildingRepository.GetByIdAsync(buildingId, Arg.Any<CancellationToken>()).Returns(building);

        var command = new ArchiveBuildingCommand(buildingId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
