using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyOS.Api.Models.Properties;
using PropertyOS.Api.Models.Leasing;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties.ParkingAssignments.Queries.Common;
using PropertyOS.Application.Properties.Security;
using PropertyOS.Application.Leasing.Security;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;

namespace PropertyOS.Tests.Integration.Api.Properties;

public class ParkingAssignmentsEndpointsTests(PostgresTestFixture fixture, WebApplicationFactory<Program> factory)
    : Module4ApiTestBase(fixture, factory)
{
    private record Assets(Guid BuildingId, Guid SpotId, Guid LeaseId, Guid TenantId);
    private record CreatedAssignment(Guid AssignmentId);

    private async Task<Assets> SeedAsync(Guid companyId, Guid? buildingId = null, ContractStatus status = ContractStatus.Active)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IBusinessClock>();
        var now = clock.UtcNow;
        var today = clock.GetJordanBusinessDate(now);
        if (buildingId == null)
        {
            var b = Building.Create(companyId, $"Building {Guid.NewGuid():N}", 2, now, null);
            db.Buildings.Add(b);
            await db.SaveChangesAsync();
            buildingId = b.Id;
        }
        // Reuse one floor when seeding multiple leases in the same building.
        var floor = await db.Floors.FirstOrDefaultAsync(f => f.BuildingId == buildingId.Value);
        if (floor == null)
        {
            floor = Floor.Create(companyId, buildingId.Value, 1, "First", now, null);
            db.Floors.Add(floor);
            await db.SaveChangesAsync();
        }
        var unit = Apartment.Create(companyId, buildingId.Value, floor.Id, Guid.NewGuid().ToString("N")[..8], 80, now, null);
        var tenant = Tenant.Create(companyId, "Parking Tenant", Guid.NewGuid().ToString("N")[..12],
            $"+96279{Random.Shared.Next(1000000, 9999999)}", now, null);
        var spot = ParkingSpot.Create(companyId, buildingId.Value, Guid.NewGuid().ToString("N")[..8], now, null);
        db.Apartments.Add(unit);
        db.Tenants.Add(tenant);
        db.ParkingSpots.Add(spot);
        await db.SaveChangesAsync();
        var lease = LeaseContract.Create(companyId, buildingId.Value, unit.Id, tenant.Id,
            $"LC-{Guid.NewGuid():N}", today.AddDays(-5), today.AddYears(1), 100, PaymentFrequency.Monthly, 1,
            now, null, status: status);
        db.LeaseContracts.Add(lease);
        await db.SaveChangesAsync();
        return new(buildingId.Value, spot.Id, lease.Id, tenant.Id);
    }

    private static Task<HttpResponseMessage> Assign(HttpClient client, Guid spot, Guid lease) =>
        client.PostAsJsonAsync($"/api/v1/parking-spots/{spot}/assignment", new AssignParkingSpotRequest(lease));

    [Fact]
    public async Task Lifecycle_PreservesHistory_AllowsReassignmentAndMultipleSpotsPerLease()
    {
        var (company, user) = await CreateTestTenantAsync("ParkingAssignment");
        var first = await SeedAsync(company);
        var second = await SeedAsync(company, first.BuildingId);
        using var client = CreateClientWithPermissions(user, company, PropertyPermissions.Read, PropertyPermissions.Update,
            LeasingPermissions.Approve);
        (await client.GetAsync($"/api/v1/parking-spots/{first.SpotId}/assignment")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var response = await Assign(client, first.SpotId, first.LeaseId);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<CreatedAssignment>())!.AssignmentId;
        id.Should().NotBeEmpty();
        var current = await client.GetFromJsonAsync<ParkingAssignmentDto>($"/api/v1/parking-spots/{first.SpotId}/assignment");
        current!.AssignmentId.Should().Be(id);
        current.TenantId.Should().Be(first.TenantId);
        current.TenantName.Should().Be("Parking Tenant");
        (await Assign(client, first.SpotId, second.LeaseId)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await client.PostAsync($"/api/v1/parking-assignments/{id}/end", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/v1/parking-assignments/{id}/end", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/v1/parking-spots/{first.SpotId}/assignment")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Assign(client, first.SpotId, second.LeaseId)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Assign(client, second.SpotId, second.LeaseId)).StatusCode.Should().Be(HttpStatusCode.Created);
        var parking = await client.GetFromJsonAsync<List<ParkingAssignmentDto>>($"/api/v1/leasing/contracts/{second.LeaseId}/parking");
        parking.Should().HaveCount(2);
        parking.Should().OnlyContain(a => a.TenantId == second.TenantId && a.LeaseContractId == second.LeaseId);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        var history = await db.ParkingAssignments.IgnoreQueryFilters().Where(a => a.ParkingSpotId == first.SpotId).ToListAsync();
        history.Should().HaveCount(2);
        history.Should().OnlyContain(a => a.DeletedAt == null);
        var ended = history.Single(a => a.Id == id);
        ended.Status.Should().Be(ParkingAssignmentStatus.Ended);
        ended.AssignedTo.Should().Be(ended.AssignedFrom);
        ended.CreatedBy.Should().Be(user);
        ended.UpdatedBy.Should().Be(user);
        history.Count(a => a.Status == ParkingAssignmentStatus.Active).Should().Be(1);
        (await db.LeaseContracts.SingleAsync(l => l.Id == first.LeaseId)).Status.Should().Be(ContractStatus.Active);
        (await db.ParkingSpots.SingleAsync(s => s.Id == first.SpotId)).IsActive.Should().BeTrue();

        var terminate = new TerminateLeaseContractRequest(TerminationType.MutualAgreement, DateTime.UtcNow,
            Reason: "Integration lifecycle test");
        (await client.PostAsJsonAsync($"/api/v1/leasing/contracts/{second.LeaseId}/terminate", terminate))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        db.ChangeTracker.Clear();
        var endedWithLease = await db.ParkingAssignments.Where(a => a.LeaseContractId == second.LeaseId).ToListAsync();
        endedWithLease.Should().HaveCount(2).And.OnlyContain(a => a.Status == ParkingAssignmentStatus.Ended
            && a.AssignedTo != null && a.DeletedAt == null);
        (await client.GetAsync($"/api/v1/parking-spots/{first.SpotId}/assignment")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ConcurrentRequests_ExactlyOneAssignmentCommits()
    {
        var (company, user) = await CreateTestTenantAsync("ParkingRace");
        var first = await SeedAsync(company);
        var second = await SeedAsync(company, first.BuildingId);
        using var clientA = CreateClientWithPermissions(user, company, PropertyPermissions.Update);
        using var clientB = CreateClientWithPermissions(user, company, PropertyPermissions.Update);
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<HttpResponseMessage> Attempt(HttpClient client, Guid lease)
        {
            await gate.Task;
            return await Assign(client, first.SpotId, lease);
        }
        var attempts = new[] { Attempt(clientA, first.LeaseId), Attempt(clientB, second.LeaseId) };
        gate.SetResult(true);
        var results = await Task.WhenAll(attempts).WaitAsync(TimeSpan.FromSeconds(30));
        results.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        results.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
        var problem = await results.Single(r => r.StatusCode == HttpStatusCode.Conflict).Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Extensions["code"].ToString().Should().Be("PARKING_ALREADY_ASSIGNED");
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        var rows = await db.ParkingAssignments.Where(a => a.ParkingSpotId == first.SpotId).ToListAsync();
        rows.Should().ContainSingle();
        rows[0].Status.Should().Be(ParkingAssignmentStatus.Active);
        new[] { first.LeaseId, second.LeaseId }.Should().Contain(rows[0].LeaseContractId);
    }

    [Fact]
    public async Task ForeignCompany_CannotAssignReadOrEnd()
    {
        var (companyA, userA) = await CreateTestTenantAsync("ParkingOwner");
        var (companyB, userB) = await CreateTestTenantAsync("ParkingOther");
        var a = await SeedAsync(companyA);
        var b = await SeedAsync(companyB);
        using var owner = CreateClientWithPermissions(userA, companyA, PropertyPermissions.Read, PropertyPermissions.Update);
        using var other = CreateClientWithPermissions(userB, companyB, PropertyPermissions.Read, PropertyPermissions.Update);
        var response = await Assign(owner, a.SpotId, a.LeaseId);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<CreatedAssignment>())!.AssignmentId;
        (await Assign(other, a.SpotId, a.LeaseId)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Assign(other, a.SpotId, b.LeaseId)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Assign(other, b.SpotId, a.LeaseId)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await other.GetAsync($"/api/v1/parking-spots/{a.SpotId}/assignment")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await other.GetAsync($"/api/v1/leasing/contracts/{a.LeaseId}/parking")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await other.PostAsync($"/api/v1/parking-assignments/{id}/end", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>();
        var rows = await db.ParkingAssignments.ToListAsync();
        rows.Should().ContainSingle();
        rows[0].Status.Should().Be(ParkingAssignmentStatus.Active);
        rows[0].CompanyId.Should().Be(companyA);
    }

    [Fact]
    public async Task AuthenticationAndPermissions_ProtectEveryEndpoint()
    {
        var (company, user) = await CreateTestTenantAsync("ParkingAuth");
        var assets = await SeedAsync(company);
        using var anonymous = CreateClientWithoutToken();
        using var forbidden = CreateClientWithPermissions(user, company);
        foreach (var (client, status) in new[] { (anonymous, HttpStatusCode.Unauthorized), (forbidden, HttpStatusCode.Forbidden) })
        {
            (await Assign(client, assets.SpotId, assets.LeaseId)).StatusCode.Should().Be(status);
            (await client.GetAsync($"/api/v1/parking-spots/{assets.SpotId}/assignment")).StatusCode.Should().Be(status);
            (await client.GetAsync($"/api/v1/leasing/contracts/{assets.LeaseId}/parking")).StatusCode.Should().Be(status);
            (await client.PostAsync($"/api/v1/parking-assignments/{Guid.NewGuid()}/end", null)).StatusCode.Should().Be(status);
        }
        using var scope = Factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>().ParkingAssignments.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BuildingMismatchAndDraftLease_AreRejectedWithoutWrites()
    {
        var (company, user) = await CreateTestTenantAsync("ParkingValidation");
        var a = await SeedAsync(company);
        var otherBuilding = await SeedAsync(company);
        var draft = await SeedAsync(company, a.BuildingId, ContractStatus.Draft);
        using var client = CreateClientWithPermissions(user, company, PropertyPermissions.Update);
        var mismatch = await Assign(client, a.SpotId, otherBuilding.LeaseId);
        mismatch.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await mismatch.Content.ReadFromJsonAsync<ProblemDetails>())!.Extensions["code"].ToString().Should().Be("PARKING_BUILDING_MISMATCH");
        (await Assign(client, a.SpotId, draft.LeaseId)).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Assign(client, a.SpotId, Guid.Empty)).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var scope = Factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<PropertyOsDbContext>().ParkingAssignments.CountAsync()).Should().Be(0);
    }
}
