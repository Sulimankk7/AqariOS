using FluentAssertions;
using Npgsql;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Tests.Integration.Infrastructure;

namespace PropertyOS.Tests.Integration.Properties;

/// <summary>
/// Module 4 Phase 1 Integration Tests.
///
/// These tests prove PostgreSQL physical invariants — NOT EF Core semantics.
/// Each test seeds using raw SQL or the superuser admin context to establish
/// a known state, then probes the physical constraint being verified.
///
/// Test scope (max 6 per specification):
///   Test37 — Floor cannot reference a building from another company via composite FK.
///   Test38 — Apartment cannot combine company A building + floor from building B.
///   Test39 — propertyos_app RLS cannot read another company's building.
///   Test40 — propertyos_app RLS cannot read another company's apartment.
///   Test41 — Building-address 1:1 constraint is physically enforced.
///   Test42 — Apartment counter trigger keeps buildings.total_apartments_count correct.
/// </summary>
[Collection("Postgres collection")]
public class PropertiesHierarchyTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private NpgsqlConnection? _appConnection;

    public PropertiesHierarchyTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _appConnection = await _fixture.AppUserDataSource!.OpenConnectionAsync();
    }

    public async Task DisposeAsync()
    {
        if (_appConnection != null)
            await _appConnection.DisposeAsync();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private async Task<(Guid CompanyId, Guid BuildingId)> SeedCompanyAndBuilding(
        NpgsqlConnection conn, string companyName, string buildingName)
    {
        // Insert company
        await using var cmdComp = conn.CreateCommand();
        cmdComp.CommandText = $@"
            INSERT INTO companies (legal_name, display_name, primary_phone, country_code, is_active, created_at, updated_at)
            VALUES (@name, @name, '+962790000099', 'JO', true, now(), now())
            RETURNING id;";
        cmdComp.Parameters.AddWithValue("name", companyName);
        var companyId = (Guid)(await cmdComp.ExecuteScalarAsync())!;

        // Insert company_settings (required by FK)
        await using var cmdSettings = conn.CreateCommand();
        cmdSettings.CommandText = $@"
            INSERT INTO company_settings (company_id, late_fee_type, fiscal_year_start_month, created_at, updated_at)
            VALUES (@cid, 'none', 1, now(), now());";
        cmdSettings.Parameters.AddWithValue("cid", companyId);
        await cmdSettings.ExecuteNonQueryAsync();

        // Insert building
        await using var cmdBuilding = conn.CreateCommand();
        cmdBuilding.CommandText = @"
            INSERT INTO buildings (company_id, name, building_type, total_floors, is_active, created_at, updated_at)
            VALUES (@cid, @name, 'residential', 5, true, now(), now())
            RETURNING id;";
        cmdBuilding.Parameters.AddWithValue("cid", companyId);
        cmdBuilding.Parameters.AddWithValue("name", buildingName);
        var buildingId = (Guid)(await cmdBuilding.ExecuteScalarAsync())!;

        return (companyId, buildingId);
    }

    private async Task<Guid> SeedBuilding(
        NpgsqlConnection conn, Guid companyId, string buildingName)
    {
        await using var cmdBuilding = conn.CreateCommand();
        cmdBuilding.CommandText = @"
            INSERT INTO buildings (company_id, name, building_type, total_floors, is_active, created_at, updated_at)
            VALUES (@cid, @name, 'residential', 5, true, now(), now())
            RETURNING id;";
        cmdBuilding.Parameters.AddWithValue("cid", companyId);
        cmdBuilding.Parameters.AddWithValue("name", buildingName);
        return (Guid)(await cmdBuilding.ExecuteScalarAsync())!;
    }

    private async Task<Guid> SeedFloor(
        NpgsqlConnection conn, Guid companyId, Guid buildingId, short floorNumber = 0)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO floors (company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at)
            VALUES (@cid, @bid, @fn, @label, 'ground', now(), now())
            RETURNING id;";
        cmd.Parameters.AddWithValue("cid", companyId);
        cmd.Parameters.AddWithValue("bid", buildingId);
        cmd.Parameters.AddWithValue("fn", floorNumber);
        cmd.Parameters.AddWithValue("label", $"Floor {floorNumber}");
        return (Guid)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<Guid> SeedApartment(
        NpgsqlConnection conn, Guid companyId, Guid buildingId, Guid floorId, string unitNumber = "101")
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO apartments (company_id, building_id, floor_id, unit_number,
                                    ownership_status, occupancy_status, area_sqm, created_at, updated_at)
            VALUES (@cid, @bid, @fid, @unit, 'company_owned', 'vacant', 75.5, now(), now())
            RETURNING id;";
        cmd.Parameters.AddWithValue("cid", companyId);
        cmd.Parameters.AddWithValue("bid", buildingId);
        cmd.Parameters.AddWithValue("fid", floorId);
        cmd.Parameters.AddWithValue("unit", unitNumber);
        return (Guid)(await cmd.ExecuteScalarAsync())!;
    }

    // =========================================================================
    // Test 37 — Floor composite FK prevents cross-company building reference
    // =========================================================================

    /// <summary>
    /// Proves that a floor cannot physically reference a building from another
    /// company even when company_id and building_id are independently valid.
    /// The composite FK fk_floors_buildings_company_building enforces:
    ///   (floors.company_id, floors.building_id) → buildings(company_id, id)
    /// A floor attempting to use company_A.id with building_B.id violates this FK.
    /// </summary>
    [Fact]
    public async Task Test37_Floor_CannotReference_BuildingFromAnotherCompany_CompositeFK()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyA_Id, buildingA_Id) = await SeedCompanyAndBuilding(adminConn, "CompanyA_T37", "BuildingA");
        var (companyB_Id, _) = await SeedCompanyAndBuilding(adminConn, "CompanyB_T37", "BuildingB");

        // Attempt to insert a floor with company_B's company_id but building_A's building_id.
        // The composite FK requires both (company_id, building_id) to match a row in buildings.
        // buildings has UNIQUE(company_id, id), and the FK references that constraint.
        // CompanyB, BuildingA is NOT a valid combination → FK violation.
        await using var cmd = adminConn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO floors (company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at)
            VALUES (@cid, @bid, 0, 'Ground', 'ground', now(), now());";
        cmd.Parameters.AddWithValue("cid", companyB_Id);    // Company B
        cmd.Parameters.AddWithValue("bid", buildingA_Id);   // Building from Company A ← mismatch

        var act = async () => await cmd.ExecuteNonQueryAsync();
        var ex = await act.Should().ThrowAsync<PostgresException>();

        // PostgreSQL FK violation is 23503 (foreign_key_violation)
        ex.Which.SqlState.Should().Be("23503",
            "composite FK fk_floors_buildings_company_building must physically reject " +
            "a floor that references a building from a different company");
    }

    // =========================================================================
    // Test 38 — Apartment composite FK prevents cross-building floor misattribution
    // =========================================================================

    /// <summary>
    /// Proves that an apartment cannot combine company A, building A, and a floor
    /// that belongs to building B of the same company.
    /// The composite FK fk_apartments_floors_company_building_floor enforces:
    ///   (apartments.company_id, apartments.building_id, apartments.floor_id)
    ///   → floors(company_id, building_id, id)
    /// </summary>
    [Fact]
    public async Task Test38_Apartment_CannotCombine_CompanyA_BuildingA_FloorFromBuildingB()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyA_Id, buildingA_Id) = await SeedCompanyAndBuilding(adminConn, "CompanyA_T38", "BuildingA_T38");
        var buildingB_Id = await SeedBuilding(adminConn, companyA_Id, "BuildingB_T38");

        // Re-seed: both buildings under the SAME company to isolate building-level mismatch
        await using var adminConn2 = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn2.OpenAsync();

        // Seed a floor under company A, building B
        var floorB_Id = await SeedFloor(adminConn2, companyA_Id, buildingB_Id, 0);

        // Now attempt to create an apartment referencing company_A + building_A + floor_from_building_B
        await using var cmd = adminConn2.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO apartments (company_id, building_id, floor_id, unit_number,
                                    ownership_status, occupancy_status, area_sqm, created_at, updated_at)
            VALUES (@cid, @bid, @fid, '101', 'company_owned', 'vacant', 55.0, now(), now());";
        cmd.Parameters.AddWithValue("cid", companyA_Id);   // Company A
        cmd.Parameters.AddWithValue("bid", buildingA_Id);  // Building A
        cmd.Parameters.AddWithValue("fid", floorB_Id);     // Floor from Building B ← mismatch

        var act = async () => await cmd.ExecuteNonQueryAsync();
        var ex = await act.Should().ThrowAsync<PostgresException>();

        ex.Which.SqlState.Should().Be("23503",
            "composite FK fk_apartments_floors_company_building_floor must reject an apartment " +
            "that references a floor from a different building");
    }

    // =========================================================================
    // Test 39 — RLS: propertyos_app cannot read another company's building
    // =========================================================================

    /// <summary>
    /// Proves that propertyos_app session set to company A cannot read buildings
    /// belonging to company B, even via IgnoreQueryFilters / raw SQL.
    /// </summary>
    [Fact]
    public async Task Test39_RlsApp_CannotRead_AnotherCompanyBuilding()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyA_Id, _) = await SeedCompanyAndBuilding(adminConn, "CompanyA_T39", "BuildingA_T39");
        var (companyB_Id, buildingB_Id) = await SeedCompanyAndBuilding(adminConn, "CompanyB_T39", "BuildingB_T39");

        // Query as Company A via the app user (FORCE RLS active)
        await using var tx = await _appConnection!.BeginTransactionAsync();
        await using var setCtx = _appConnection.CreateCommand();
        setCtx.CommandText = $"SET LOCAL app.current_company_id = '{companyA_Id}';";
        setCtx.Transaction = tx;
        await setCtx.ExecuteNonQueryAsync();

        await using var selectCmd = _appConnection.CreateCommand();
        selectCmd.CommandText = "SELECT id FROM buildings WHERE id = @bid;";
        selectCmd.Parameters.AddWithValue("bid", buildingB_Id);
        selectCmd.Transaction = tx;

        var result = await selectCmd.ExecuteScalarAsync();

        result.Should().BeNull(
            "RLS policy rls_buildings_app must prevent Company A from reading Company B's building");

        await tx.RollbackAsync();
    }

    // =========================================================================
    // Test 40 — RLS: propertyos_app cannot read another company's apartment
    // =========================================================================

    /// <summary>
    /// Proves that propertyos_app session set to company A cannot read apartments
    /// belonging to company B.
    /// </summary>
    [Fact]
    public async Task Test40_RlsApp_CannotRead_AnotherCompanyApartment()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyA_Id, _) = await SeedCompanyAndBuilding(adminConn, "CompanyA_T40", "BuildingA_T40");
        var (companyB_Id, buildingB_Id) = await SeedCompanyAndBuilding(adminConn, "CompanyB_T40", "BuildingB_T40");

        var floorB_Id = await SeedFloor(adminConn, companyB_Id, buildingB_Id, 0);
        var apartmentB_Id = await SeedApartment(adminConn, companyB_Id, buildingB_Id, floorB_Id, "501");

        // Query as Company A
        await using var tx = await _appConnection!.BeginTransactionAsync();
        await using var setCtx = _appConnection.CreateCommand();
        setCtx.CommandText = $"SET LOCAL app.current_company_id = '{companyA_Id}';";
        setCtx.Transaction = tx;
        await setCtx.ExecuteNonQueryAsync();

        await using var selectCmd = _appConnection.CreateCommand();
        selectCmd.CommandText = "SELECT id FROM apartments WHERE id = @aid;";
        selectCmd.Parameters.AddWithValue("aid", apartmentB_Id);
        selectCmd.Transaction = tx;

        var result = await selectCmd.ExecuteScalarAsync();

        result.Should().BeNull(
            "RLS policy rls_apartments_app must prevent Company A from reading Company B's apartment");

        await tx.RollbackAsync();
    }

    // =========================================================================
    // Test 41 — Building-address 1:1 constraint is physically enforced
    // =========================================================================

    /// <summary>
    /// Proves that a second address for the same building is physically rejected
    /// by the unique index uq_building_addresses_building_id.
    /// </summary>
    [Fact]
    public async Task Test41_BuildingAddress_OneToOne_PhysicallyEnforced()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyId, buildingId) = await SeedCompanyAndBuilding(adminConn, "CompanyA_T41", "BuildingA_T41");

        // Insert first address — must succeed
        await using var cmd1 = adminConn.CreateCommand();
        cmd1.CommandText = @"
            INSERT INTO building_addresses (building_id, company_id, governorate, district, created_at, updated_at)
            VALUES (@bid, @cid, 'amman', 'Jabal Amman', now(), now());";
        cmd1.Parameters.AddWithValue("bid", buildingId);
        cmd1.Parameters.AddWithValue("cid", companyId);
        await cmd1.ExecuteNonQueryAsync();

        // Insert second address for the same building — must be rejected (unique violation 23505)
        await using var cmd2 = adminConn.CreateCommand();
        cmd2.CommandText = @"
            INSERT INTO building_addresses (building_id, company_id, governorate, district, created_at, updated_at)
            VALUES (@bid, @cid, 'zarqa', 'Zarqa District', now(), now());";
        cmd2.Parameters.AddWithValue("bid", buildingId);
        cmd2.Parameters.AddWithValue("cid", companyId);

        var act = async () => await cmd2.ExecuteNonQueryAsync();
        var ex = await act.Should().ThrowAsync<PostgresException>();

        ex.Which.SqlState.Should().Be("23505",
            "uq_building_addresses_building_id must physically enforce 1:1 cardinality");
    }

    // =========================================================================
    // Test 42 — Apartment counter trigger maintains buildings.total_apartments_count
    // =========================================================================

    /// <summary>
    /// Proves that:
    ///   1. Inserting an apartment increments buildings.total_apartments_count.
    ///   2. Soft-deleting that apartment decrements it back to 0.
    /// This validates the trg_apartments_counters trigger fires correctly.
    /// </summary>
    [Fact]
    public async Task Test42_ApartmentCounterTrigger_UpdatesBuilding_TotalApartmentsCount()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyId, buildingId) = await SeedCompanyAndBuilding(adminConn, "CompanyA_T42", "BuildingA_T42");
        var floorId = await SeedFloor(adminConn, companyId, buildingId, 0);

        // Initial counter must be 0
        await using var countCmd0 = adminConn.CreateCommand();
        countCmd0.CommandText = "SELECT total_apartments_count FROM buildings WHERE id = @bid;";
        countCmd0.Parameters.AddWithValue("bid", buildingId);
        var count0 = (int)(await countCmd0.ExecuteScalarAsync())!;
        count0.Should().Be(0, "no apartments inserted yet");

        // Insert two apartments
        var apt1Id = await SeedApartment(adminConn, companyId, buildingId, floorId, "101");
        var apt2Id = await SeedApartment(adminConn, companyId, buildingId, floorId, "102");

        await using var countCmd2 = adminConn.CreateCommand();
        countCmd2.CommandText = "SELECT total_apartments_count FROM buildings WHERE id = @bid;";
        countCmd2.Parameters.AddWithValue("bid", buildingId);
        var count2 = (int)(await countCmd2.ExecuteScalarAsync())!;
        count2.Should().Be(2, "trigger must have incremented counter twice on INSERT");

        // Soft-delete one apartment
        await using var softDeleteCmd = adminConn.CreateCommand();
        softDeleteCmd.CommandText = "UPDATE apartments SET deleted_at = now() WHERE id = @aid;";
        softDeleteCmd.Parameters.AddWithValue("aid", apt1Id);
        await softDeleteCmd.ExecuteNonQueryAsync();

        await using var countCmd1 = adminConn.CreateCommand();
        countCmd1.CommandText = "SELECT total_apartments_count FROM buildings WHERE id = @bid;";
        countCmd1.Parameters.AddWithValue("bid", buildingId);
        var count1 = (int)(await countCmd1.ExecuteScalarAsync())!;
        count1.Should().Be(1, "trigger must have decremented counter on soft-delete");

        // Also verify floors.apartments_count is maintained
        await using var floorCountCmd = adminConn.CreateCommand();
        floorCountCmd.CommandText = "SELECT apartments_count FROM floors WHERE id = @fid;";
        floorCountCmd.Parameters.AddWithValue("fid", floorId);
        var floorCount = (int)(await floorCountCmd.ExecuteScalarAsync())!;
        floorCount.Should().Be(1, "floor counter must also be decremented by the same trigger");
    }
}
