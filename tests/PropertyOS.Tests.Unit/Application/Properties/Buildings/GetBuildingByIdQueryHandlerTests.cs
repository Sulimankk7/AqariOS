using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Buildings.Queries.GetBuildingById;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Buildings;

public class GetBuildingByIdQueryHandlerTests
{
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly GetBuildingByIdQueryHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();

    public GetBuildingByIdQueryHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _handler = new GetBuildingByIdQueryHandler(_buildingRepository, _tenantContext);
    }

    [Fact]
    public async Task Handle_WithExistingBuilding_ShouldReturnBuildingDto()
    {
        // Arrange
        var buildingId = Guid.CreateVersion7();
        var building = Building.Create(CompanyId, "Test Building", 10, DateTimeOffset.UtcNow, null, id: buildingId);
        _buildingRepository.GetByIdAsync(buildingId, Arg.Any<CancellationToken>()).Returns(building);

        var query = new GetBuildingByIdQuery(buildingId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(buildingId);
        result.Name.Should().Be("Test Building");
        result.CompanyId.Should().Be(CompanyId);
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var query = new GetBuildingByIdQuery(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        _buildingRepository.GetByIdAsync(buildingId, Arg.Any<CancellationToken>()).Returns((Building?)null);

        var query = new GetBuildingByIdQuery(buildingId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForDifferentCompany_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var buildingId = Guid.CreateVersion7();

        var building = Building.Create(otherCompanyId, "Other Building", 10, DateTimeOffset.UtcNow, null, id: buildingId);
        _buildingRepository.GetByIdAsync(buildingId, Arg.Any<CancellationToken>()).Returns(building);

        var query = new GetBuildingByIdQuery(buildingId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingInactive_ShouldThrowNotFoundException()
    {
        // Arrange
        var buildingId = Guid.CreateVersion7();
        var building = Building.Create(CompanyId, "Deactivated Building", 10, DateTimeOffset.UtcNow, null, id: buildingId);
        building.Deactivate(DateTimeOffset.UtcNow, null);

        _buildingRepository.GetByIdAsync(buildingId, Arg.Any<CancellationToken>()).Returns(building);

        var query = new GetBuildingByIdQuery(buildingId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
