using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Infrastructure.Leasing;

[Collection("Postgres collection")]
public class LeaseContractDatabaseMechanismTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;

    public LeaseContractDatabaseMechanismTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record LeasingSeed(Guid CompanyId, Guid BuildingId, Guid ApartmentId, Guid TenantId);

    private async Task<LeasingSeed> SeedPrerequisites()
    {
        var companyId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var floorId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO companies (id, name, type, created_at) VALUES (@cId, 'Test', 'Owner', now());
            INSERT INTO buildings (id, company_id, name, created_at) VALUES (@bId, @cId, 'B', now());
            INSERT INTO floors (id, building_id, number, created_at) VALUES (@fId, @bId, 1, now());
            INSERT INTO apartments (id, floor_id, building_id, company_id, number, type, bedrooms, bathrooms, base_rent_amount, size_sqm, created_at) 
                VALUES (@aId, @fId, @bId, @cId, '101', 'Residential', 1, 1, 100, 100, now());
            INSERT INTO tenants (id, company_id, type, first_name, last_name, phone_number, created_at) 
                VALUES (@tId, @cId, 'Personal', 'T', '1', '123', now());
        ";
        cmd.Parameters.Add(new NpgsqlParameter("cId", companyId));
        cmd.Parameters.Add(new NpgsqlParameter("bId", buildingId));
        cmd.Parameters.Add(new NpgsqlParameter("fId", floorId));
        cmd.Parameters.Add(new NpgsqlParameter("aId", apartmentId));
        cmd.Parameters.Add(new NpgsqlParameter("tId", tenantId));
        await cmd.ExecuteNonQueryAsync();

        return new LeasingSeed(companyId, buildingId, apartmentId, tenantId);
    }

    [Fact]
    public async Task Uq_Lease_Contracts_One_Active_Per_Apartment_Enforced()
    {
        var seed = await SeedPrerequisites();

        var contract1 = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-1", new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Active);
        _fixture.Context.LeaseContracts.Add(contract1);
        await _fixture.Context.SaveChangesAsync();

        var contract2 = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-2", new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Active);
        _fixture.Context.LeaseContracts.Add(contract2);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _fixture.Context.SaveChangesAsync());
        var pgEx = ex.InnerException as PostgresException;
        Assert.NotNull(pgEx);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pgEx.SqlState);
        Assert.Contains("uq_lease_contracts_one_active_per_apartment", pgEx.ConstraintName);
    }

    [Fact]
    public async Task Uq_Lease_Contracts_Prior_Contract_Id_Enforced()
    {
        var seed = await SeedPrerequisites();

        var contract1 = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-1", new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Active);
        _fixture.Context.LeaseContracts.Add(contract1);
        await _fixture.Context.SaveChangesAsync();

        var contract2 = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-2", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), contract1.Id, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Draft);
        _fixture.Context.LeaseContracts.Add(contract2);
        await _fixture.Context.SaveChangesAsync();

        var contract3 = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-3", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), contract1.Id, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Draft);
        _fixture.Context.LeaseContracts.Add(contract3);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _fixture.Context.SaveChangesAsync());
        var pgEx = ex.InnerException as PostgresException;
        Assert.NotNull(pgEx);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pgEx.SqlState);
        Assert.Contains("uq_lease_contracts_prior_contract_id", pgEx.ConstraintName);
    }

    [Fact]
    public async Task Partial_Index_Soft_Delete_Predicate_Allows_Deleted_Duplicates()
    {
        var seed = await SeedPrerequisites();

        var contract1 = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-1", new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Active);
        contract1.SoftDelete(DateTimeOffset.UtcNow, Guid.NewGuid());
        _fixture.Context.LeaseContracts.Add(contract1);
        await _fixture.Context.SaveChangesAsync();

        var contract2 = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-2", new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Active);
        _fixture.Context.LeaseContracts.Add(contract2);
        await _fixture.Context.SaveChangesAsync();
        
        var deletedContracts = await _fixture.Context.LeaseContracts.IgnoreQueryFilters().CountAsync(c => c.ApartmentId == seed.ApartmentId && c.Status == ContractStatus.Active && c.DeletedAt != null);
        Assert.Equal(1, deletedContracts);

        var activeContracts = await _fixture.Context.LeaseContracts.CountAsync(c => c.ApartmentId == seed.ApartmentId && c.Status == ContractStatus.Active);
        Assert.Equal(1, activeContracts);
    }

    [Fact]
    public async Task Occupancy_Trigger_Activation()
    {
        var seed = await SeedPrerequisites();

        var apartmentBefore = await _fixture.Context.Apartments.AsNoTracking().SingleAsync(a => a.Id == seed.ApartmentId);
        Assert.Equal(OccupancyStatus.Vacant, apartmentBefore.OccupancyStatus);

        var contract = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-1", new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Draft);
        _fixture.Context.LeaseContracts.Add(contract);
        await _fixture.Context.SaveChangesAsync();

        var apartmentDraft = await _fixture.Context.Apartments.AsNoTracking().SingleAsync(a => a.Id == seed.ApartmentId);
        Assert.Equal(OccupancyStatus.Vacant, apartmentDraft.OccupancyStatus);

        await _fixture.Context.Database.ExecuteSqlRawAsync("UPDATE lease_contracts SET status = 'Active' WHERE id = {0}", contract.Id);

        var apartmentActive = await _fixture.Context.Apartments.AsNoTracking().SingleAsync(a => a.Id == seed.ApartmentId);
        Assert.Equal(OccupancyStatus.Occupied, apartmentActive.OccupancyStatus);
    }

    [Fact]
    public async Task Occupancy_Trigger_Leaving_Active()
    {
        var seed = await SeedPrerequisites();

        var contract = LeaseContract.Create(seed.CompanyId, seed.BuildingId, seed.ApartmentId, seed.TenantId, "LC-1", new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1), 100, PaymentFrequency.Monthly, 1, DateTimeOffset.UtcNow, Guid.NewGuid(), null, LegalRegime.Standard, TenantType.Personal, 100, ContractStatus.Active);
        _fixture.Context.LeaseContracts.Add(contract);
        await _fixture.Context.SaveChangesAsync();

        var apartmentActive = await _fixture.Context.Apartments.AsNoTracking().SingleAsync(a => a.Id == seed.ApartmentId);
        Assert.Equal(OccupancyStatus.Occupied, apartmentActive.OccupancyStatus);

        await _fixture.Context.Database.ExecuteSqlRawAsync("UPDATE lease_contracts SET status = 'Terminated' WHERE id = {0}", contract.Id);

        var apartmentTerminated = await _fixture.Context.Apartments.AsNoTracking().SingleAsync(a => a.Id == seed.ApartmentId);
        Assert.Equal(OccupancyStatus.Vacant, apartmentTerminated.OccupancyStatus);
    }
}
