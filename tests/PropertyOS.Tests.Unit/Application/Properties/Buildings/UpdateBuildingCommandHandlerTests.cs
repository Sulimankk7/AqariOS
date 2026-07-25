using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Buildings.Commands.UpdateBuilding;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Buildings;

public class UpdateBuildingCommandHandlerTests
{
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly UpdateBuildingCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public UpdateBuildingCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);

        _handler = new UpdateBuildingCommandHandler(
            _buildingRepository,
            _tenantContext,
            _currentUserContext);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldUpdateBuildingAndPreserveAddressIdentity()
    {
        // Arrange
        var buildingId = Guid.CreateVersion7();
        var existingBuilding = Building.Create(CompanyId, "Old Name", 10, DateTimeOffset.UtcNow, UserId, id: buildingId);

        var createdAt = DateTimeOffset.UtcNow.AddDays(-10);
        var oldAddress = BuildingAddress.Create(buildingId, CompanyId, Governorate.Amman, "Amman", createdAt, "Old Area", "Old Street", null, null, "11111");
        existingBuilding.SetAddress(oldAddress);
        existingBuilding.Address.Should().NotBeNull();

        var address = existingBuilding.Address!;
        var beforeAddressId = address.Id;
        var beforeBuildingId = address.BuildingId;
        var beforeCompanyId = address.CompanyId;
        var beforeCreatedAt = address.CreatedAt;

        _buildingRepository.GetByIdAsync(buildingId, Arg.Any<CancellationToken>()).Returns(existingBuilding);
        _buildingRepository.ExistsByCodeAsync(CompanyId, "BLD-NEW", Arg.Any<CancellationToken>()).Returns(false);

        var command = new UpdateBuildingCommand(
            Id: buildingId,
            Name: "New Building Name",
            BuildingType: BuildingType.Commercial,
            InternalCode: "BLD-NEW",
            ConstructionYear: 2026,
            GpsLatitude: 32.0m,
            GpsLongitude: 36.0m,
            AddressGovernorate: Governorate.Zarqa,
            AddressCity: "Zarqa",
            AddressNeighborhood: "New Zarqa",
            AddressStreet: "New Street",
            AddressPostalCode: "13111"
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Core Building properties updated
        existingBuilding.Name.Should().Be("New Building Name");
        existingBuilding.BuildingType.Should().Be(BuildingType.Commercial);
        existingBuilding.InternalCode.Should().Be("BLD-NEW");
        existingBuilding.ConstructionYear.Should().Be((short)2026);
        existingBuilding.UpdatedBy.Should().Be(UserId);

        // Assert - Address Identity Preserved
        existingBuilding.Address.Should().NotBeNull();
        existingBuilding.Address!.Id.Should().Be(beforeAddressId);
        existingBuilding.Address.BuildingId.Should().Be(beforeBuildingId);
        existingBuilding.Address.CompanyId.Should().Be(beforeCompanyId);
        existingBuilding.Address.CreatedAt.Should().Be(beforeCreatedAt);

        // Assert - Mutable address fields changed
        existingBuilding.Address.Governorate.Should().Be(Governorate.Zarqa);
        existingBuilding.Address.District.Should().Be("Zarqa");
        existingBuilding.Address.Area.Should().Be("New Zarqa");
        existingBuilding.Address.StreetName.Should().Be("New Street");
        existingBuilding.Address.PostalCode.Should().Be("13111");
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);

        var command = new UpdateBuildingCommand(
            Guid.NewGuid(), "New Name", BuildingType.Residential, null, null, null, null, Governorate.Amman, "Amman", "Area", null, null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        _buildingRepository.GetByIdAsync(buildingId, Arg.Any<CancellationToken>()).Returns((Building?)null);

        var command = new UpdateBuildingCommand(
            buildingId, "New Name", BuildingType.Residential, null, null, null, null, Governorate.Amman, "Amman", "Area", null, null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForDifferentCompany_ShouldThrowNotFoundException()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var buildingId = Guid.CreateVersion7();
        var building = Building.Create(otherCompanyId, "Old Name", 10, DateTimeOffset.UtcNow, null, id: buildingId);

        _buildingRepository.GetByIdAsync(buildingId, Arg.Any<CancellationToken>()).Returns(building);

        var command = new UpdateBuildingCommand(
            buildingId, "New Name", BuildingType.Residential, null, null, null, null, Governorate.Amman, "Amman", "Area", null, null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithDuplicateInternalCode_ShouldThrowConflictException()
    {
        // Arrange
        var buildingId = Guid.CreateVersion7();
        var existingBuilding = Building.Create(CompanyId, "Old Name", 10, DateTimeOffset.UtcNow, null, id: buildingId);

        _buildingRepository.GetByIdAsync(buildingId, Arg.Any<CancellationToken>()).Returns(existingBuilding);
        _buildingRepository.ExistsByCodeAsync(CompanyId, "DUP-CODE", Arg.Any<CancellationToken>()).Returns(true);

        var command = new UpdateBuildingCommand(
            buildingId, "New Name", BuildingType.Residential, "DUP-CODE", null, null, null, Governorate.Amman, "Amman", "Area", null, null);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
