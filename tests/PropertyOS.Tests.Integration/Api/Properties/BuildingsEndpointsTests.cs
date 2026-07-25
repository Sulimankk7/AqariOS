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
using PropertyOS.Application.Properties.Buildings.Queries.Common;
using PropertyOS.Application.Properties.Security;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Api.Properties;

public class BuildingsEndpointsTests : Module4ApiTestBase
{
    public BuildingsEndpointsTests(PostgresTestFixture fixture, WebApplicationFactory<Program> factory)
        : base(fixture, factory)
    {
    }

    [Fact]
    public async Task Building_CompleteCrudLifecycle_SucceedsOverHttp()
    {
        // Arrange
        var (companyId, userId) = await CreateTestTenantAsync("BuildingsCrud");
        var client = CreateClientWithPermissions(userId, companyId,
            PropertyPermissions.Read, PropertyPermissions.Create, PropertyPermissions.Update, PropertyPermissions.Delete);

        var createReq = new CreateBuildingRequest
        {
            Name = "Grand Horizon Tower",
            TotalFloors = 10,
            BuildingType = BuildingType.Commercial,
            InternalCode = "BLD-GH-01",
            ConstructionYear = 2024,
            AddressGovernorate = Governorate.Amman,
            AddressCity = "Amman",
            AddressNeighborhood = "Seventh Circle"
        };

        // 1. CREATE Building (POST /api/v1/buildings)
        var createResp = await client.PostAsJsonAsync("/api/v1/buildings", createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Guid buildingId;
        // Verify creation in DB via fresh superuser DbContext scope
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var createdBuilding = await db.Buildings
                .Include(b => b.Address)
                .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.InternalCode == "BLD-GH-01");

            createdBuilding.Should().NotBeNull();
            createdBuilding!.Name.Should().Be("Grand Horizon Tower");
            createdBuilding.TotalFloors.Should().Be(10);
            createdBuilding.Address.Should().NotBeNull();
            createdBuilding.Address.District.Should().Be("Amman");
            createdBuilding.Address.Area.Should().Be("Seventh Circle");

            buildingId = createdBuilding.Id;
        }

        // 2. LIST Buildings (GET /api/v1/buildings)
        var listResp = await client.GetAsync("/api/v1/buildings");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var buildingsList = await listResp.Content.ReadFromJsonAsync<List<BuildingDto>>();
        buildingsList.Should().NotBeNull();
        buildingsList.Should().ContainSingle(b => b.Id == buildingId && b.Name == "Grand Horizon Tower");

        // 3. GET Building BY ID (GET /api/v1/buildings/{id})
        var getResp = await client.GetAsync($"/api/v1/buildings/{buildingId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var buildingDto = await getResp.Content.ReadFromJsonAsync<BuildingDto>();
        buildingDto.Should().NotBeNull();
        buildingDto!.Id.Should().Be(buildingId);
        buildingDto.Name.Should().Be("Grand Horizon Tower");

        // 4. UPDATE Building (PUT /api/v1/buildings/{id})
        var updateReq = new UpdateBuildingRequest
        {
            Name = "Grand Horizon Tower Updated",
            BuildingType = BuildingType.MixedUse,
            InternalCode = "BLD-GH-01-UPD",
            AddressGovernorate = Governorate.Amman,
            AddressCity = "Amman",
            AddressNeighborhood = "Seventh Circle Updated"
        };

        var updateResp = await client.PutAsJsonAsync($"/api/v1/buildings/{buildingId}", updateReq);
        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5. ARCHIVE Building (DELETE /api/v1/buildings/{id})
        var archiveResp = await client.DeleteAsync($"/api/v1/buildings/{buildingId}");
        archiveResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 6. VERIFY SOFT DELETE in DB (via fresh DbContext scope) and List Exclusion
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var archivedBuildingInDb = await db.Buildings
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == buildingId);

            archivedBuildingInDb.Should().NotBeNull();
            archivedBuildingInDb!.IsActive.Should().BeFalse();
            archivedBuildingInDb.DeletedAt.Should().NotBeNull();
            archivedBuildingInDb.DeletedBy.Should().Be(userId);
        }

        // List should no longer contain archived building
        var postArchiveListResp = await client.GetAsync("/api/v1/buildings");
        var postArchiveList = await postArchiveListResp.Content.ReadFromJsonAsync<List<BuildingDto>>();
        postArchiveList.Should().NotContain(b => b.Id == buildingId);
    }
}
