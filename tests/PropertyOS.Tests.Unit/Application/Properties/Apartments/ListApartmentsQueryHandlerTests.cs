using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Apartments.Queries.ListApartments;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Apartments;

public class ListApartmentsQueryHandlerTests
{
    private readonly IApartmentRepository _apartmentRepository = Substitute.For<IApartmentRepository>();
    private readonly IFloorRepository _floorRepository = Substitute.For<IFloorRepository>();
    private readonly IBuildingRepository _buildingRepository = Substitute.For<IBuildingRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ListApartmentsQueryHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();
    private static readonly Guid FloorId = Guid.CreateVersion7();

    public ListApartmentsQueryHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _handler = new ListApartmentsQueryHandler(
            _apartmentRepository,
            _floorRepository,
            _buildingRepository,
            _tenantContext);
    }

    [Fact]
    public async Task Handle_ByFloorId_ShouldReturnMappedApartments()
    {
        // Arrange
        var floor = Floor.Create(CompanyId, BuildingId, 1, "First Floor", DateTimeOffset.UtcNow, null, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, FloorId);
        _floorRepository.GetByIdAsync(FloorId, Arg.Any<CancellationToken>()).Returns(floor);

        var apt1 = Apartment.Create(CompanyId, BuildingId, FloorId, "101", 100m, DateTimeOffset.UtcNow, null);
        var apt2 = Apartment.Create(CompanyId, BuildingId, FloorId, "102", 110m, DateTimeOffset.UtcNow, null);

        _apartmentRepository.ListByFloorIdAsync(FloorId, Arg.Any<CancellationToken>())
            .Returns(new List<Apartment> { apt1, apt2 });

        var query = new ListApartmentsQuery(FloorId: FloorId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].UnitNumber.Should().Be("101");
        result[1].UnitNumber.Should().Be("102");
    }

    [Fact]
    public async Task Handle_ByBuildingId_ShouldReturnMappedApartments()
    {
        // Arrange
        var building = Building.Create(CompanyId, "Parent Building", 10, DateTimeOffset.UtcNow, null, id: BuildingId);
        _buildingRepository.GetByIdAsync(BuildingId, Arg.Any<CancellationToken>()).Returns(building);

        var apt1 = Apartment.Create(CompanyId, BuildingId, FloorId, "101", 100m, DateTimeOffset.UtcNow, null);

        _apartmentRepository.ListByBuildingIdAsync(BuildingId, Arg.Any<CancellationToken>())
            .Returns(new List<Apartment> { apt1 });

        var query = new ListApartmentsQuery(BuildingId: BuildingId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].UnitNumber.Should().Be("101");
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var query = new ListApartmentsQuery();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenFloorNotFound_ShouldThrowNotFound()
    {
        // Arrange
        _floorRepository.GetByIdAsync(FloorId, Arg.Any<CancellationToken>()).Returns((Floor?)null);
        var query = new ListApartmentsQuery(FloorId: FloorId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForDifferentCompanyFloor_ShouldThrowNotFound()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var floor = Floor.Create(otherCompanyId, BuildingId, 1, "First Floor", DateTimeOffset.UtcNow, null, FloorType.Regular);
        typeof(Floor).GetProperty("Id")?.SetValue(floor, FloorId);

        _floorRepository.GetByIdAsync(FloorId, Arg.Any<CancellationToken>()).Returns(floor);
        var query = new ListApartmentsQuery(FloorId: FloorId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
