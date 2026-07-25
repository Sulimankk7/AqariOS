using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyOS.Api.Models.Properties;
using PropertyOS.Application.Properties.Security;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Api.Properties;

public class PropertyTenantIsolationTests : Module4ApiTestBase
{
    public PropertyTenantIsolationTests(PostgresTestFixture fixture, WebApplicationFactory<Program> factory)
        : base(fixture, factory)
    {
    }

    [Fact]
    public async Task CrossTenant_ResourceAccess_IsBlockedByTenantIsolationAndRls()
    {
        // Setup Tenant A and Tenant B
        var (companyA, userA) = await CreateTestTenantAsync("CompanyA");
        var (companyB, userB) = await CreateTestTenantAsync("CompanyB");

        var clientA = CreateClientWithPermissions(userA, companyA,
            PropertyPermissions.Read, PropertyPermissions.Create, PropertyPermissions.Update, PropertyPermissions.Delete);

        var clientB = CreateClientWithPermissions(userB, companyB,
            PropertyPermissions.Read, PropertyPermissions.Create, PropertyPermissions.Update, PropertyPermissions.Delete);

        // 1. Company A Creates Building, Floor, Apartment, and ParkingSpot
        var createBldReq = new CreateBuildingRequest
        {
            Name = "Company A Secret Tower",
            TotalFloors = 5,
            BuildingType = BuildingType.Commercial,
            AddressGovernorate = Governorate.Amman,
            AddressCity = "Amman",
            AddressNeighborhood = "Abdoun"
        };
        await clientA.PostAsJsonAsync("/api/v1/buildings", createBldReq);

        Guid bldAId, floorAId, aptAId, spotAId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var building = await db.Buildings.FirstAsync(b => b.CompanyId == companyA);
            bldAId = building.Id;
        }

        var createFloorReq = new CreateFloorRequest { FloorNumber = 1, FloorLabel = "Floor 1", FloorType = FloorType.Regular };
        await clientA.PostAsJsonAsync($"/api/v1/buildings/{bldAId}/floors", createFloorReq);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var floor = await db.Floors.FirstAsync(f => f.BuildingId == bldAId);
            floorAId = floor.Id;
        }

        var createAptReq = new CreateApartmentRequest { UnitNumber = "A-101", AreaSqm = 100m };
        await clientA.PostAsJsonAsync($"/api/v1/floors/{floorAId}/apartments", createAptReq);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var apt = await db.Apartments.FirstAsync(a => a.FloorId == floorAId);
            aptAId = apt.Id;
        }

        var createSpotReq = new CreateParkingSpotRequest { SpotCode = "P-A1", ParkingType = ParkingType.Covered };
        await clientA.PostAsJsonAsync($"/api/v1/buildings/{bldAId}/parking-spots", createSpotReq);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var spot = await db.ParkingSpots.FirstAsync(s => s.BuildingId == bldAId);
            spotAId = spot.Id;
        }

        // 2. Client B attempts to GET Company A resources -> Expected 404 (Not Found due to tenant scoping)
        (await clientB.GetAsync($"/api/v1/buildings/{bldAId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await clientB.GetAsync($"/api/v1/floors/{floorAId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await clientB.GetAsync($"/api/v1/apartments/{aptAId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await clientB.GetAsync($"/api/v1/parking-spots/{spotAId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 3. Client B attempts to UPDATE Company A resources -> Expected 404
        var updateBldReq = new UpdateBuildingRequest { Name = "Hacked Name", BuildingType = BuildingType.Residential, AddressGovernorate = Governorate.Amman, AddressCity = "Amman", AddressNeighborhood = "Abdoun" };
        (await clientB.PutAsJsonAsync($"/api/v1/buildings/{bldAId}", updateBldReq)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 4. Client B attempts to DELETE Company A resources -> Expected 404
        (await clientB.DeleteAsync($"/api/v1/buildings/{bldAId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await clientB.DeleteAsync($"/api/v1/floors/{floorAId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 5. Hierarchy Forgery Prevention: Client B attempts to create child resources under Company A's parent entities
        var forgeFloorReq = new CreateFloorRequest { FloorNumber = 9, FloorLabel = "Forged Floor", FloorType = FloorType.Regular };
        var forgeFloorResp = await clientB.PostAsJsonAsync($"/api/v1/buildings/{bldAId}/floors", forgeFloorReq);
        forgeFloorResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var forgeAptReq = new CreateApartmentRequest { UnitNumber = "FORGED-99", AreaSqm = 50m };
        var forgeAptResp = await clientB.PostAsJsonAsync($"/api/v1/floors/{floorAId}/apartments", forgeAptReq);
        forgeAptResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var forgeSpotReq = new CreateParkingSpotRequest { SpotCode = "FORGED-P", ParkingType = ParkingType.Standard };
        var forgeSpotResp = await clientB.PostAsJsonAsync($"/api/v1/buildings/{bldAId}/parking-spots", forgeSpotReq);
        forgeSpotResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
