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
using PropertyOS.Application.Properties.Apartments.Queries.Common;
using PropertyOS.Application.Properties.Security;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Api.Properties;

public class ApartmentsEndpointsTests : Module4ApiTestBase
{
    public ApartmentsEndpointsTests(PostgresTestFixture fixture, WebApplicationFactory<Program> factory)
        : base(fixture, factory)
    {
    }

    [Fact]
    public async Task Apartment_CompleteCrudLifecycle_SucceedsOverHttp()
    {
        // Arrange
        var (companyId, userId) = await CreateTestTenantAsync("AptCrud");
        var client = CreateClientWithPermissions(userId, companyId,
            PropertyPermissions.Read, PropertyPermissions.Create, PropertyPermissions.Update, PropertyPermissions.Delete);

        // 1. Create Parent Building
        var createBldReq = new CreateBuildingRequest
        {
            Name = "Apartment Test Residence",
            TotalFloors = 3,
            BuildingType = BuildingType.Residential,
            AddressGovernorate = Governorate.Amman,
            AddressCity = "Amman",
            AddressNeighborhood = "Dabouq"
        };
        await client.PostAsJsonAsync("/api/v1/buildings", createBldReq);

        Guid buildingId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            buildingId = (await db.Buildings.FirstAsync(b => b.CompanyId == companyId)).Id;
        }

        // 2. Create Parent Floor
        var createFloorReq = new CreateFloorRequest
        {
            FloorNumber = 1,
            FloorLabel = "First Floor",
            FloorType = FloorType.Regular
        };
        await client.PostAsJsonAsync($"/api/v1/buildings/{buildingId}/floors", createFloorReq);

        Guid floorId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            floorId = (await db.Floors.FirstAsync(f => f.BuildingId == buildingId)).Id;
        }

        // 3. CREATE Apartment (POST /api/v1/floors/{floorId}/apartments)
        var createAptReq = new CreateApartmentRequest
        {
            UnitNumber = "101",
            AreaSqm = 150.5m,
            OwnershipStatus = OwnershipStatus.CompanyOwned,
            Bedrooms = 3,
            Bathrooms = 2,
            BaseRentAmount = 750.00m,
            BaseRentCurrency = "JOD"
        };

        var createResp = await client.PostAsJsonAsync($"/api/v1/floors/{floorId}/apartments", createAptReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Guid apartmentId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var apt = await db.Apartments.FirstOrDefaultAsync(a => a.FloorId == floorId && a.UnitNumber == "101");
            apt.Should().NotBeNull();
            apt!.AreaSqm.Should().Be(150.5m);
            apartmentId = apt.Id;
        }

        // 4. LIST Apartments (GET /api/v1/apartments?buildingId=...&floorId=...)
        var listResp = await client.GetAsync($"/api/v1/apartments?buildingId={buildingId}&floorId={floorId}");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var aptList = await listResp.Content.ReadFromJsonAsync<List<ApartmentDto>>();
        aptList.Should().NotBeNull();
        aptList.Should().ContainSingle(a => a.Id == apartmentId && a.UnitNumber == "101");

        // 5. GET Apartment BY ID (GET /api/v1/apartments/{id})
        var getResp = await client.GetAsync($"/api/v1/apartments/{apartmentId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var aptDto = await getResp.Content.ReadFromJsonAsync<ApartmentDto>();
        aptDto.Should().NotBeNull();
        aptDto!.Id.Should().Be(apartmentId);

        // 6. UPDATE Apartment (PUT /api/v1/apartments/{id})
        var updateReq = new UpdateApartmentRequest
        {
            BaseRentAmount = 800.00m,
            BaseRentCurrency = "JOD"
        };
        var updateResp = await client.PutAsJsonAsync($"/api/v1/apartments/{apartmentId}", updateReq);
        updateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 7. ARCHIVE Apartment (DELETE /api/v1/apartments/{id})
        var archiveResp = await client.DeleteAsync($"/api/v1/apartments/{apartmentId}");
        archiveResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 8. VERIFY SOFT DELETE
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
            var archivedApt = await db.Apartments.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == apartmentId);
            archivedApt.Should().NotBeNull();
            archivedApt!.DeletedAt.Should().NotBeNull();
            archivedApt.DeletedBy.Should().Be(userId);
        }
    }
}
