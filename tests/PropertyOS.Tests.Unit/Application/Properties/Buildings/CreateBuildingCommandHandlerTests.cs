using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Buildings.Commands.CreateBuilding;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Buildings;

public class CreateBuildingCommandHandlerTests
{
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly CreateBuildingCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public CreateBuildingCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);

        _handler = new CreateBuildingCommandHandler(
            _buildingRepository,
            _tenantContext,
            _currentUserContext);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldAddBuilding()
    {
        // Arrange
        var command = new CreateBuildingCommand(
            Name: "Test Building",
            TotalFloors: 10,
            BuildingType: BuildingType.Residential,
            InternalCode: "BLD-01",
            ConstructionYear: 2025,
            GpsLatitude: 31.95m,
            GpsLongitude: 35.91m,
            AddressGovernorate: Governorate.Amman,
            AddressCity: "Amman",
            AddressNeighborhood: "Khalda",
            AddressStreet: "Wasfi Al Tal",
            AddressPostalCode: "11953"
        );

        _buildingRepository.ExistsByCodeAsync(CompanyId, "BLD-01", Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _buildingRepository.Received(1).AddAsync(Arg.Is<Building>(b =>
            b.Name == "Test Building" &&
            b.CompanyId == CompanyId &&
            b.Id != Guid.Empty &&
            b.CreatedBy == UserId &&
            b.Address != null &&
            b.Address.CompanyId == CompanyId &&
            b.Address.BuildingId == b.Id &&
            b.Address.Governorate == Governorate.Amman &&
            b.Address.District == "Amman" &&
            b.Address.Area == "Khalda"
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);

        var command = new CreateBuildingCommand(
            Name: "Test Building",
            TotalFloors: 10,
            BuildingType: BuildingType.Residential,
            InternalCode: null,
            ConstructionYear: null,
            GpsLatitude: null,
            GpsLongitude: null,
            AddressGovernorate: Governorate.Amman,
            AddressCity: "Amman",
            AddressNeighborhood: "Khalda",
            AddressStreet: null,
            AddressPostalCode: null
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithDuplicateInternalCode_ShouldThrowConflictException()
    {
        // Arrange
        var command = new CreateBuildingCommand(
            Name: "Test Building",
            TotalFloors: 10,
            BuildingType: BuildingType.Residential,
            InternalCode: "DUP-01",
            ConstructionYear: null,
            GpsLatitude: null,
            GpsLongitude: null,
            AddressGovernorate: Governorate.Amman,
            AddressCity: "Amman",
            AddressNeighborhood: "Khalda",
            AddressStreet: null,
            AddressPostalCode: null
        );

        _buildingRepository.ExistsByCodeAsync(CompanyId, "DUP-01", Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
        ex.Message.Should().Contain("already exists");
    }
}
