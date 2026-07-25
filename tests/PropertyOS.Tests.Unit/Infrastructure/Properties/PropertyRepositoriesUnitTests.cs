using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Infrastructure.Properties.Repositories;
using Xunit;

namespace PropertyOS.Tests.Unit.Infrastructure.Properties;

public class PropertyRepositoriesUnitTests
{
    private static PropertyOsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task BuildingRepository_Should_Add_And_Retrieve_Building_With_Address()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new BuildingRepository(dbContext);
        var companyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var buildingId = Guid.CreateVersion7();
        var building = Building.Create(companyId, "Al-Noor Tower", 10, now, null, BuildingType.Residential, "BLD-01", id: buildingId);
        var address = BuildingAddress.Create(building.Id, companyId, Governorate.Amman, "Seventh Circle", now, "Al-Rabiya");

        building.SetAddress(address);

        // Act
        await repository.AddAsync(building, default);
        await dbContext.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(building.Id, default);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Al-Noor Tower");
        retrieved.InternalCode.Should().Be("BLD-01");
        retrieved.TotalFloors.Should().Be(10);
        retrieved.Address.Should().NotBeNull();
        retrieved.Address!.District.Should().Be("Seventh Circle");
    }

    [Fact]
    public async Task BuildingRepository_Should_Check_Name_And_Code_Uniqueness_And_Exclude_Soft_Deleted()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new BuildingRepository(dbContext);
        var companyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var building1 = Building.Create(companyId, "Unique Name", 5, now, null, BuildingType.Residential, "CODE-100");
        var building2 = Building.Create(companyId, "Deleted Building", 3, now, null, BuildingType.Commercial, "CODE-200");
        building2.SoftDelete(now, null);

        await repository.AddAsync(building1, default);
        await repository.AddAsync(building2, default);
        await dbContext.SaveChangesAsync();

        // Act
        var nameExists = await repository.ExistsByNameAsync(companyId, "unique name", default);
        var codeExists = await repository.ExistsByCodeAsync(companyId, "code-100", default);
        var buildings = await repository.ListByCompanyIdAsync(companyId, default);
        var count = await repository.GetCountByCompanyIdAsync(companyId, default);

        // Assert
        nameExists.Should().BeTrue();
        codeExists.Should().BeTrue();
        buildings.Should().HaveCount(1);
        buildings[0].Name.Should().Be("Unique Name");
        count.Should().Be(1);
    }

    [Fact]
    public async Task FloorRepository_Should_Add_List_By_Building_And_Check_FloorNumber_Uniqueness()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new FloorRepository(dbContext);
        var companyId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var floor1 = Floor.Create(companyId, buildingId, 0, "Ground Floor", now, null, FloorType.Ground);
        var floor2 = Floor.Create(companyId, buildingId, 1, "First Floor", now, null, FloorType.Regular);

        await repository.AddAsync(floor1, default);
        await repository.AddAsync(floor2, default);
        await dbContext.SaveChangesAsync();

        // Act
        var exists = await repository.ExistsByFloorNumberAsync(buildingId, 0, default);
        var notExists = await repository.ExistsByFloorNumberAsync(buildingId, 2, default);
        var floors = await repository.ListByBuildingIdAsync(buildingId, default);

        // Assert
        exists.Should().BeTrue();
        notExists.Should().BeFalse();
        floors.Should().HaveCount(2);
        floors[0].FloorNumber.Should().Be(0);
        floors[1].FloorNumber.Should().Be(1);
    }

    [Fact]
    public async Task ApartmentRepository_Should_Add_Retrieve_And_Check_UnitNumber_Uniqueness()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new ApartmentRepository(dbContext);
        var companyId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var floorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var apartment = Apartment.Create(companyId, buildingId, floorId, "101", 125.5m, now, null, OwnershipStatus.CompanyOwned, null, null, 2, 2, 500m, "JOD");

        await repository.AddAsync(apartment, default);
        await dbContext.SaveChangesAsync();

        // Act
        var retrievedByCompany = await repository.GetByIdAsync(apartment.Id, companyId, default);
        var retrievedByRls = await repository.GetByIdAsync(apartment.Id, default);
        var unitExists = await repository.UnitNumberExistsInBuildingAsync(buildingId, "101", default);
        var bldExists = await repository.BuildingExistsAsync(buildingId, companyId, default);

        // Assert
        retrievedByCompany.Should().NotBeNull();
        retrievedByRls.Should().NotBeNull();
        retrievedByRls!.UnitNumber.Should().Be("101");
        retrievedByRls.AreaSqm.Should().Be(125.5m);
        retrievedByRls.BaseRentAmount.Should().Be(500m);
        unitExists.Should().BeTrue();
    }

    [Fact]
    public async Task ParkingSpotRepository_Should_Add_List_By_Building_And_Check_Code_Uniqueness()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new ParkingSpotRepository(dbContext);
        var companyId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var spot = ParkingSpot.Create(companyId, buildingId, "P-01", now, null, ParkingType.Standard, null, "Basement 1");

        await repository.AddAsync(spot, default);
        await dbContext.SaveChangesAsync();

        // Act
        var spotExists = await repository.ExistsBySpotCodeAsync(buildingId, "p-01", default);
        var spots = await repository.ListByBuildingIdAsync(buildingId, default);

        // Assert
        spotExists.Should().BeTrue();
        spots.Should().HaveCount(1);
        spots[0].SpotCode.Should().Be("P-01");
        spots[0].LocationDescription.Should().Be("Basement 1");
    }
}
