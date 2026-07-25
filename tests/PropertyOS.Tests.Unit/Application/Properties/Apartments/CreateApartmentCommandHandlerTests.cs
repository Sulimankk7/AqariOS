using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Apartments.Commands.CreateApartment;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Apartments;

public class CreateApartmentCommandHandlerTests
{
    private readonly IApartmentRepository _apartmentRepository = Substitute.For<IApartmentRepository>();
    private readonly IFloorRepository _floorRepository = Substitute.For<IFloorRepository>();
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly CreateApartmentCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();
    private static readonly Guid FloorId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.NewGuid();

    public CreateApartmentCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);

        _handler = new CreateApartmentCommandHandler(
            _apartmentRepository,
            _floorRepository,
            _buildingRepository,
            _tenantContext,
            _currentUserContext);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldAddApartment()
    {
        // Arrange
        var floor = Floor.Create(CompanyId, BuildingId, 1, "First Floor", DateTimeOffset.UtcNow, UserId, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, FloorId);

        var building = Building.Create(CompanyId, "Parent Building", 10, DateTimeOffset.UtcNow, UserId, id: BuildingId);

        _floorRepository.GetByIdAsync(FloorId, Arg.Any<CancellationToken>()).Returns(floor);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);
        _apartmentRepository.UnitNumberExistsInBuildingAsync(BuildingId, "101", Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateApartmentCommand(
            FloorId: FloorId,
            UnitNumber: "101",
            AreaSqm: 120m,
            OwnershipStatus: OwnershipStatus.CompanyOwned,
            Bedrooms: 2,
            Bathrooms: 2,
            BaseRentAmount: 500m,
            BaseRentCurrency: "JOD"
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _apartmentRepository.Received(1).AddAsync(Arg.Is<Apartment>(a =>
            a.CompanyId == CompanyId &&
            a.BuildingId == BuildingId &&
            a.FloorId == FloorId &&
            a.UnitNumber == "101" &&
            a.AreaSqm == 120m &&
            a.Bedrooms == 2 &&
            a.Bathrooms == 2 &&
            a.BaseRentAmount == 500m &&
            a.OccupancyStatus == OccupancyStatus.Vacant &&
            a.CreatedBy == UserId
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var command = new CreateApartmentCommand(FloorId, "101", 100m);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenFloorNotFound_ShouldThrowNotFound()
    {
        // Arrange
        _floorRepository.GetByIdAsync(FloorId, Arg.Any<CancellationToken>()).Returns((Floor?)null);
        var command = new CreateApartmentCommand(FloorId, "101", 100m);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForDifferentCompanyFloor_ShouldThrowNotFound()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var floor = Floor.Create(otherCompanyId, BuildingId, 1, "First Floor", DateTimeOffset.UtcNow, null, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, FloorId);

        _floorRepository.GetByIdAsync(FloorId, Arg.Any<CancellationToken>()).Returns(floor);
        var command = new CreateApartmentCommand(FloorId, "101", 100m);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBuildingInactive_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var floor = Floor.Create(CompanyId, BuildingId, 1, "First Floor", DateTimeOffset.UtcNow, null, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, FloorId);

        var building = Building.Create(CompanyId, "Inactive Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);
        building.Deactivate(DateTimeOffset.UtcNow, null);

        _floorRepository.GetByIdAsync(FloorId, Arg.Any<CancellationToken>()).Returns(floor);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);

        var command = new CreateApartmentCommand(FloorId, "101", 100m);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithDuplicateUnitNumberInBuilding_ShouldThrowConflictException()
    {
        // Arrange
        var floor = Floor.Create(CompanyId, BuildingId, 1, "First Floor", DateTimeOffset.UtcNow, null, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, FloorId);

        var building = Building.Create(CompanyId, "Parent Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);

        _floorRepository.GetByIdAsync(FloorId, Arg.Any<CancellationToken>()).Returns(floor);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);
        _apartmentRepository.UnitNumberExistsInBuildingAsync(BuildingId, "101", Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateApartmentCommand(FloorId, "101", 100m);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
        ex.Message.Should().Contain("already exists");
    }
}
