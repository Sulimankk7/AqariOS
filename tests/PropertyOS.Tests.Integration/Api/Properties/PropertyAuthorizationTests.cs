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

public class PropertyAuthorizationTests : Module4ApiTestBase
{
    public PropertyAuthorizationTests(PostgresTestFixture fixture, WebApplicationFactory<Program> factory)
        : base(fixture, factory)
    {
    }

    [Fact]
    public async Task Request_WithoutToken_Returns401Unauthorized()
    {
        var client = CreateClientWithoutToken();

        var bldResp = await client.GetAsync("/api/v1/buildings");
        bldResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var postResp = await client.PostAsJsonAsync("/api/v1/buildings", new CreateBuildingRequest { Name = "Unauth" });
        postResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithoutRequiredPermission_Returns403Forbidden()
    {
        var (companyId, userId) = await CreateTestTenantAsync("AuthCheck");

        // Client with ONLY properties.read permission
        var readOnlyClient = CreateClientWithPermissions(userId, companyId, PropertyPermissions.Read);

        // GET should succeed (200)
        var getResp = await readOnlyClient.GetAsync("/api/v1/buildings");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // POST should be forbidden (403)
        var postReq = new CreateBuildingRequest
        {
            Name = "Forbidden Tower",
            TotalFloors = 2,
            AddressGovernorate = Governorate.Amman,
            AddressCity = "Amman",
            AddressNeighborhood = "Abdoun"
        };
        var postResp = await readOnlyClient.PostAsJsonAsync("/api/v1/buildings", postReq);
        postResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Request_WithPropertiesManage_SatisfiesAllModule4Policies()
    {
        var (companyId, userId) = await CreateTestTenantAsync("ManageOverride");

        // Client with ONLY properties.manage permission
        var manageClient = CreateClientWithPermissions(userId, companyId, PropertyPermissions.Manage);

        // 1. CREATE (POST)
        var createReq = new CreateBuildingRequest
        {
            Name = "Superuser Tower",
            TotalFloors = 12,
            BuildingType = BuildingType.Commercial,
            AddressGovernorate = Governorate.Amman,
            AddressCity = "Amman",
            AddressNeighborhood = "Seventh Circle"
        };
        var createResp = await manageClient.PostAsJsonAsync("/api/v1/buildings", createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Guid buildingId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            buildingId = (await db.Buildings.FirstAsync(b => b.CompanyId == companyId)).Id;
        }

        // 2. READ (GET)
        var getResp = await manageClient.GetAsync($"/api/v1/buildings/{buildingId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. UPDATE (PUT)
        var updateReq = new UpdateBuildingRequest
        {
            Name = "Superuser Tower Updated",
            BuildingType = BuildingType.Commercial,
            AddressGovernorate = Governorate.Amman,
            AddressCity = "Amman",
            AddressNeighborhood = "Seventh Circle"
        };
        var updateResp = await manageClient.PutAsJsonAsync($"/api/v1/buildings/{buildingId}", updateReq);
        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. DELETE (DELETE)
        var deleteResp = await manageClient.DeleteAsync($"/api/v1/buildings/{buildingId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
