using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PropertyOS.Api.Models.Properties;
using PropertyOS.Application.Properties.Security;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Api.Properties;

public class PropertyValidationAndErrorTests : Module4ApiTestBase
{
    public PropertyValidationAndErrorTests(PostgresTestFixture fixture, WebApplicationFactory<Program> factory)
        : base(fixture, factory)
    {
    }

    [Fact]
    public async Task CreateBuilding_WithMissingRequiredFields_Returns400BadRequest()
    {
        var (companyId, userId) = await CreateTestTenantAsync("ValidationTest");
        var client = CreateClientWithPermissions(userId, companyId, PropertyPermissions.Create);

        // Missing Name and City
        var invalidReq = new CreateBuildingRequest
        {
            Name = "",
            TotalFloors = 0,
            AddressCity = ""
        };

        var response = await client.PostAsJsonAsync("/api/v1/buildings", invalidReq);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNonExistentBuilding_Returns404NotFound()
    {
        var (companyId, userId) = await CreateTestTenantAsync("NotFoundTest");
        var client = CreateClientWithPermissions(userId, companyId, PropertyPermissions.Read);

        var randomId = Guid.CreateVersion7();
        var response = await client.GetAsync($"/api/v1/buildings/{randomId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBuilding_WithDuplicateInternalCode_Returns409Conflict()
    {
        var (companyId, userId) = await CreateTestTenantAsync("ConflictTest");
        var client = CreateClientWithPermissions(userId, companyId, PropertyPermissions.Create);

        var req1 = new CreateBuildingRequest
        {
            Name = "Tower One",
            TotalFloors = 5,
            InternalCode = "BLD-DUP-01",
            AddressGovernorate = Governorate.Amman,
            AddressCity = "Amman",
            AddressNeighborhood = "Abdoun"
        };
        var resp1 = await client.PostAsJsonAsync("/api/v1/buildings", req1);
        resp1.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var req2 = new CreateBuildingRequest
        {
            Name = "Tower Two",
            TotalFloors = 5,
            InternalCode = "BLD-DUP-01", // Duplicate code
            AddressGovernorate = Governorate.Amman,
            AddressCity = "Amman",
            AddressNeighborhood = "Abdoun"
        };
        var resp2 = await client.PostAsJsonAsync("/api/v1/buildings", req2);
        resp2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
