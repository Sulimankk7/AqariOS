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
using PropertyOS.Application.Properties.Floors.Queries.Common;
using PropertyOS.Application.Properties.Security;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Api.Properties;

public class FloorsEndpointsTests : Module4ApiTestBase
{
    public FloorsEndpointsTests(PostgresTestFixture fixture, WebApplicationFactory<Program> factory)
        : base(fixture, factory)
    {
    }

    [Fact]
    public async Task Floor_CompleteCrudLifecycle_SucceedsOverHttp()
    {
        // Arrange
        var (companyId, userId) = await CreateTestTenantAsync("FloorsCrud");
        var client = CreateClientWithPermissions(userId, companyId,
            PropertyPermissions.Read, PropertyPermissions.Create, PropertyPermissions.Update, PropertyPermissions.Delete);

        // First Create Parent Building
        var createBldReq = new CreateBuildingRequest
        {
            Name = "Floor Test Plaza",
            TotalFloors = 5,
            BuildingType = BuildingType.Commercial,
            AddressGovernorate = Governorate.Amman,
            AddressCity = "Amman",
            AddressNeighborhood = "Shmeisani"
        };
        await client.PostAsJsonAsync("/api/v1/buildings", createBldReq);

        Guid buildingId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var building = await db.Buildings.FirstAsync(b => b.CompanyId == companyId);
            buildingId = building.Id;
        }

        // 1. CREATE Floor (POST /api/v1/buildings/{buildingId}/floors)
        var createFloorReq = new CreateFloorRequest
        {
            FloorNumber = 1,
            FloorLabel = "Floor 1 - Offices",
            FloorType = FloorType.Regular
        };

        var createResp = await client.PostAsJsonAsync($"/api/v1/buildings/{buildingId}/floors", createFloorReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Guid floorId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var createdFloor = await db.Floors.FirstOrDefaultAsync(f => f.BuildingId == buildingId && f.FloorNumber == 1);
            createdFloor.Should().NotBeNull();
            createdFloor!.FloorLabel.Should().Be("Floor 1 - Offices");
            floorId = createdFloor.Id;
        }

        // 2. LIST Floors BY BUILDING (GET /api/v1/buildings/{buildingId}/floors)
        var listResp = await client.GetAsync($"/api/v1/buildings/{buildingId}/floors");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var floorsList = await listResp.Content.ReadFromJsonAsync<List<FloorDto>>();
        floorsList.Should().NotBeNull();
        floorsList.Should().ContainSingle(f => f.Id == floorId && f.FloorNumber == 1);

        // 3. GET Floor BY ID (GET /api/v1/floors/{id})
        var getResp = await client.GetAsync($"/api/v1/floors/{floorId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var floorDto = await getResp.Content.ReadFromJsonAsync<FloorDto>();
        floorDto.Should().NotBeNull();
        floorDto!.Id.Should().Be(floorId);

        // 4. UPDATE Floor (PUT /api/v1/floors/{id})
        var updateReq = new UpdateFloorRequest
        {
            FloorLabel = "Floor 1 - Executive Suite",
            FloorType = FloorType.Roof
        };
        var updateResp = await client.PutAsJsonAsync($"/api/v1/floors/{floorId}", updateReq);
        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5. ARCHIVE Floor (DELETE /api/v1/floors/{id})
        var archiveResp = await client.DeleteAsync($"/api/v1/floors/{floorId}");
        archiveResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 6. VERIFY SOFT DELETE
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var archivedFloor = await db.Floors.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == floorId);
            archivedFloor.Should().NotBeNull();
            archivedFloor!.DeletedAt.Should().NotBeNull();
            archivedFloor.DeletedBy.Should().Be(userId);
        }
    }
}
