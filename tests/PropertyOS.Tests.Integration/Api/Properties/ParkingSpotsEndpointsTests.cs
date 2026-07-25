using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyOS.Api.Models.Properties;
using PropertyOS.Application.Properties.ParkingSpots.Queries.Common;
using PropertyOS.Application.Properties.Security;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Api.Properties;

public class ParkingSpotsEndpointsTests : Module4ApiTestBase
{
    public ParkingSpotsEndpointsTests(PostgresTestFixture fixture, WebApplicationFactory<Program> factory)
        : base(fixture, factory)
    {
    }

    [Fact]
    public async Task ParkingSpot_CompleteCrudLifecycle_SucceedsOverHttp()
    {
        // Arrange
        var (companyId, userId) = await CreateTestTenantAsync("ParkingCrud");
        var client = CreateClientWithPermissions(userId, companyId,
            PropertyPermissions.Read, PropertyPermissions.Create, PropertyPermissions.Update, PropertyPermissions.Delete);

        // 1. Create Parent Building
        var createBldReq = new CreateBuildingRequest
        {
            Name = "Parking Test Building",
            TotalFloors = 4,
            BuildingType = BuildingType.Commercial,
            AddressGovernorate = Governorate.Amman,
            AddressCity = "Amman",
            AddressNeighborhood = "Mecca Street"
        };
        await client.PostAsJsonAsync("/api/v1/buildings", createBldReq);

        Guid buildingId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            buildingId = (await db.Buildings.FirstAsync(b => b.CompanyId == companyId)).Id;
        }

        // 2. CREATE ParkingSpot (POST /api/v1/buildings/{buildingId}/parking-spots)
        var createSpotReq = new CreateParkingSpotRequest
        {
            SpotCode = "P1-05",
            ParkingType = ParkingType.Covered,
            LocationDescription = "Basement B1, Column 5"
        };

        var createResp = await client.PostAsJsonAsync($"/api/v1/buildings/{buildingId}/parking-spots", createSpotReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Guid spotId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var spot = await db.ParkingSpots.FirstOrDefaultAsync(s => s.BuildingId == buildingId && s.SpotCode == "P1-05");
            spot.Should().NotBeNull();
            spot!.ParkingType.Should().Be(ParkingType.Covered);
            spotId = spot.Id;
        }

        // 3. LIST ParkingSpots BY BUILDING (GET /api/v1/buildings/{buildingId}/parking-spots)
        var listResp = await client.GetAsync($"/api/v1/buildings/{buildingId}/parking-spots");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var spotsList = await listResp.Content.ReadFromJsonAsync<List<ParkingSpotDto>>();
        spotsList.Should().NotBeNull();
        spotsList.Should().ContainSingle(s => s.Id == spotId && s.SpotCode == "P1-05");

        // 4. GET ParkingSpot BY ID (GET /api/v1/parking-spots/{id})
        var getResp = await client.GetAsync($"/api/v1/parking-spots/{spotId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var spotDto = await getResp.Content.ReadFromJsonAsync<ParkingSpotDto>();
        spotDto.Should().NotBeNull();
        spotDto!.Id.Should().Be(spotId);

        // 5. UPDATE ParkingSpot (PUT /api/v1/parking-spots/{id})
        var updateReq = new UpdateParkingSpotRequest
        {
            SpotCode = "P1-05-EV",
            ParkingType = ParkingType.DisabledAccess,
            LocationDescription = "Basement B1, Column 5 with Access Ramp"
        };
        var updateResp = await client.PutAsJsonAsync($"/api/v1/parking-spots/{spotId}", updateReq);
        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 6. ARCHIVE ParkingSpot (DELETE /api/v1/parking-spots/{id})
        var archiveResp = await client.DeleteAsync($"/api/v1/parking-spots/{spotId}");
        archiveResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 7. VERIFY SOFT DELETE
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var archivedSpot = await db.ParkingSpots.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == spotId);
            archivedSpot.Should().NotBeNull();
            archivedSpot!.DeletedAt.Should().NotBeNull();
            archivedSpot.DeletedBy.Should().Be(userId);
        }
    }
}
