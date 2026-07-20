using FluentAssertions;
using Npgsql;
using PropertyOS.Tests.Integration.Infrastructure;

namespace PropertyOS.Tests.Integration.Properties;

[Collection("Postgres collection")]
public class ParkingPhysicalInvariantTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private NpgsqlConnection? _appConnection;

    public ParkingPhysicalInvariantTests(PostgresTestFixture fixture)
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
        await using var cmdComp = conn.CreateCommand();
        cmdComp.CommandText = $@"
            INSERT INTO companies (legal_name, display_name, primary_phone, country_code, is_active, created_at, updated_at)
            VALUES (@name, @name, '+962790000099', 'JO', true, now(), now())
            RETURNING id;";
        cmdComp.Parameters.AddWithValue("name", companyName);
        var companyId = (Guid)(await cmdComp.ExecuteScalarAsync())!;

        await using var cmdSettings = conn.CreateCommand();
        cmdSettings.CommandText = $@"
            INSERT INTO company_settings (company_id, late_fee_type, fiscal_year_start_month, created_at, updated_at)
            VALUES (@cid, 'none', 1, now(), now());";
        cmdSettings.Parameters.AddWithValue("cid", companyId);
        await cmdSettings.ExecuteNonQueryAsync();

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

    private async Task<Guid> SeedBuilding(NpgsqlConnection conn, Guid companyId, string buildingName)
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

    private async Task<Guid> SeedFloor(NpgsqlConnection conn, Guid companyId, Guid buildingId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO floors (company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at)
            VALUES (@cid, @bid, 1, 'Floor 1', 'regular', now(), now())
            RETURNING id;";
        cmd.Parameters.AddWithValue("cid", companyId);
        cmd.Parameters.AddWithValue("bid", buildingId);
        return (Guid)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<Guid> SeedApartment(NpgsqlConnection conn, Guid companyId, Guid buildingId, Guid floorId, string unitNumber = "101")
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

    private async Task<Guid> SeedParkingSpot(NpgsqlConnection conn, Guid companyId, Guid buildingId, string spotCode, Guid? defaultApartmentId = null)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO parking_spots (company_id, building_id, default_apartment_id, spot_code, parking_type, location_description, is_active, created_at, updated_at)
            VALUES (@cid, @bid, @aptId, @code, 'standard', 'Level 1', true, now(), now())
            RETURNING id;";
        cmd.Parameters.AddWithValue("cid", companyId);
        cmd.Parameters.AddWithValue("bid", buildingId);
        cmd.Parameters.AddWithValue("aptId", (object?)defaultApartmentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("code", spotCode);
        return (Guid)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<Guid> SeedLeaseContract(NpgsqlConnection conn, Guid companyId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            WITH new_tenant AS (
                INSERT INTO tenants (company_id, name, national_id, phone, created_at, updated_at) 
                VALUES (@cid, 'T ' || left(gen_random_uuid()::text, 8), left(gen_random_uuid()::text, 12), '+962' || left(gen_random_uuid()::text, 10), now(), now()) RETURNING id
            ),
            new_bldg AS (
                INSERT INTO buildings (company_id, name, building_type, total_floors, created_at, updated_at) 
                VALUES (@cid, 'B', 'residential', 1, now(), now()) RETURNING id
            ),
            new_floor AS (
                INSERT INTO floors (company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at) 
                SELECT @cid, id, 1, 'Floor 1', 'regular', now(), now() FROM new_bldg RETURNING id, building_id
            ),
            new_apt AS (
                INSERT INTO apartments (floor_id, building_id, company_id, unit_number, occupancy_status, bedrooms, bathrooms, base_rent_amount, area_sqm, created_at, updated_at) 
                SELECT id, building_id, @cid, '101', 'vacant', 1, 1, 100, 100, now(), now() FROM new_floor RETURNING id, building_id
            )
            INSERT INTO lease_contracts (company_id, building_id, apartment_id, tenant_id, contract_number, start_date, end_date, monthly_rent_amount, payment_frequency, payment_due_day, created_at, legal_regime, tenant_type, status, security_deposit_amount, updated_at) 
            SELECT @cid, a.building_id, a.id, t.id, 'LC-' || left(gen_random_uuid()::text, 8), CURRENT_DATE, CURRENT_DATE + interval '1 year', 100, 'monthly', 1, now(), 'standard', 'personal', 'draft', 100, now()
            FROM new_apt a CROSS JOIN new_tenant t RETURNING id;
        ";
        cmd.Parameters.AddWithValue("cid", companyId);
        return (Guid)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<Guid> SeedParkingAssignment(NpgsqlConnection conn, Guid companyId, Guid parkingSpotId, string status)
    {
        var leaseId = await SeedLeaseContract(conn, companyId);
        
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO parking_assignments (company_id, parking_spot_id, lease_contract_id, status, assigned_from, created_at, updated_at)
            VALUES (@cid, @spotId, @leaseId, @status::parking_assignment_status_enum, CURRENT_DATE, now(), now())
            RETURNING id;";
        cmd.Parameters.AddWithValue("cid", companyId);
        cmd.Parameters.AddWithValue("spotId", parkingSpotId);
        cmd.Parameters.AddWithValue("leaseId", leaseId);
        cmd.Parameters.AddWithValue("status", status);
        return (Guid)(await cmd.ExecuteScalarAsync())!;
    }

    // =========================================================================
    // 1. PARKING SPOT CROSS-COMPANY BUILDING REJECTION
    // =========================================================================
    [Fact]
    public async Task Test43_ParkingSpot_CannotReference_BuildingFromAnotherCompany_CompositeFK()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyA, buildingA) = await SeedCompanyAndBuilding(adminConn, "CompA", "BldgA");
        var (companyB, _) = await SeedCompanyAndBuilding(adminConn, "CompB", "BldgB");

        await using var cmd = adminConn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO parking_spots (company_id, building_id, spot_code, parking_type, is_active, created_at, updated_at)
            VALUES (@cid, @bid, 'A1', 'standard', true, now(), now());";
        cmd.Parameters.AddWithValue("cid", companyB); // Comp B
        cmd.Parameters.AddWithValue("bid", buildingA); // Bldg from Comp A

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        ex.SqlState.Should().Be("23503"); // foreign_key_violation
    }

    // =========================================================================
    // 2. DEFAULT APARTMENT HIERARCHY REJECTION
    // =========================================================================
    [Fact]
    public async Task Test44_ParkingSpot_DefaultApartment_HierarchyRejection()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyA, buildingA) = await SeedCompanyAndBuilding(adminConn, "CompA", "BldgA");
        var floorA = await SeedFloor(adminConn, companyA, buildingA);
        var aptA = await SeedApartment(adminConn, companyA, buildingA, floorA, "101");

        var buildingB = await SeedBuilding(adminConn, companyA, "BldgB");
        var floorB = await SeedFloor(adminConn, companyA, buildingB);
        var aptB = await SeedApartment(adminConn, companyA, buildingB, floorB, "201");

        await using var cmd = adminConn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO parking_spots (company_id, building_id, default_apartment_id, spot_code, parking_type, is_active, created_at, updated_at)
            VALUES (@cid, @bid, @aptId, 'A1', 'standard', true, now(), now());";
        cmd.Parameters.AddWithValue("cid", companyA);
        cmd.Parameters.AddWithValue("bid", buildingA); // Building A
        cmd.Parameters.AddWithValue("aptId", aptB);    // Apartment B (belongs to Building B)

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        ex.SqlState.Should().Be("23503"); // foreign_key_violation
    }

    // =========================================================================
    // 3. ACTIVE PARKING ASSIGNMENT UNIQUENESS
    // =========================================================================
    [Fact]
    public async Task Test45_ParkingAssignment_Active_UniqueConstraint()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyA, buildingA) = await SeedCompanyAndBuilding(adminConn, "CompA", "BldgA");
        var spotId = await SeedParkingSpot(adminConn, companyA, buildingA, "A1");

        // Insert first active assignment
        await SeedParkingAssignment(adminConn, companyA, spotId, "active");

        // Attempt to insert second active assignment
        await using var cmd = adminConn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO parking_assignments (company_id, parking_spot_id, lease_contract_id, status, assigned_from, created_at, updated_at)
            VALUES (@cid, @spotId, @leaseId, 'active'::parking_assignment_status_enum, CURRENT_DATE, now(), now());";
        cmd.Parameters.AddWithValue("cid", companyA);
        cmd.Parameters.AddWithValue("spotId", spotId);
        cmd.Parameters.AddWithValue("leaseId", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        ex.SqlState.Should().Be("23505"); // unique_violation
        ex.ConstraintName.Should().Be("uq_parking_assignments_active_spot");
    }

    // =========================================================================
    // 4. ENDED ASSIGNMENT HISTORY ALLOWED
    // =========================================================================
    [Fact]
    public async Task Test46_ParkingAssignment_Ended_HistoryAllowed()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyA, buildingA) = await SeedCompanyAndBuilding(adminConn, "CompA", "BldgA");
        var spotId = await SeedParkingSpot(adminConn, companyA, buildingA, "A1");

        // Insert ended assignment
        await SeedParkingAssignment(adminConn, companyA, spotId, "ended");

        // Insert second ended assignment (should succeed because filter is status = 'active')
        await SeedParkingAssignment(adminConn, companyA, spotId, "ended");

        await using var verifyCmd = adminConn.CreateCommand();
        verifyCmd.CommandText = "SELECT COUNT(*) FROM parking_assignments WHERE parking_spot_id = @spotId";
        verifyCmd.Parameters.AddWithValue("spotId", spotId);
        var count = (long)(await verifyCmd.ExecuteScalarAsync())!;

        count.Should().Be(2);
    }

    // =========================================================================
    // 5. PARKING SPOT RLS TENANT ISOLATION
    // =========================================================================
    [Fact]
    public async Task Test47_ParkingSpot_RLS_TenantIsolation()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyA, buildingA) = await SeedCompanyAndBuilding(adminConn, "CompA", "BldgA");
        var spotA = await SeedParkingSpot(adminConn, companyA, buildingA, "A1");

        var (companyB, buildingB) = await SeedCompanyAndBuilding(adminConn, "CompB", "BldgB");
        var spotB = await SeedParkingSpot(adminConn, companyB, buildingB, "B1");

        await using var tx = await _appConnection!.BeginTransactionAsync();

        await using var setupCmd = _appConnection.CreateCommand();
        setupCmd.Transaction = tx;
        setupCmd.CommandText = "SELECT set_config('app.current_company_id', @cid, true);";
        setupCmd.Parameters.AddWithValue("cid", companyA.ToString());
        await setupCmd.ExecuteNonQueryAsync();

        await using var selectCmd = _appConnection.CreateCommand();
        selectCmd.Transaction = tx;
        selectCmd.CommandText = "SELECT id FROM parking_spots;";
        
        var foundSpots = new List<Guid>();
        await using var reader = await selectCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            foundSpots.Add(reader.GetGuid(0));
        }

        foundSpots.Should().ContainSingle().Which.Should().Be(spotA);
    }

    // =========================================================================
    // 6. PARKING ASSIGNMENT RLS TENANT ISOLATION
    // =========================================================================
    [Fact]
    public async Task Test48_ParkingAssignment_RLS_TenantIsolation()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyA, buildingA) = await SeedCompanyAndBuilding(adminConn, "CompA", "BldgA");
        var spotA = await SeedParkingSpot(adminConn, companyA, buildingA, "A1");
        var assignmentA = await SeedParkingAssignment(adminConn, companyA, spotA, "active");

        var (companyB, buildingB) = await SeedCompanyAndBuilding(adminConn, "CompB", "BldgB");
        var spotB = await SeedParkingSpot(adminConn, companyB, buildingB, "B1");
        var assignmentB = await SeedParkingAssignment(adminConn, companyB, spotB, "active");

        await using var tx = await _appConnection!.BeginTransactionAsync();

        await using var setupCmd = _appConnection.CreateCommand();
        setupCmd.Transaction = tx;
        setupCmd.CommandText = "SELECT set_config('app.current_company_id', @cid, true);";
        setupCmd.Parameters.AddWithValue("cid", companyA.ToString());
        await setupCmd.ExecuteNonQueryAsync();

        await using var selectCmd = _appConnection.CreateCommand();
        selectCmd.Transaction = tx;
        selectCmd.CommandText = "SELECT id FROM parking_assignments;";
        
        var foundAssignments = new List<Guid>();
        await using var reader = await selectCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            foundAssignments.Add(reader.GetGuid(0));
        }

        foundAssignments.Should().ContainSingle().Which.Should().Be(assignmentA);
    }

    // =========================================================================
    // 7. PROPERTYOS_APP HARD DELETE DENIED
    // =========================================================================
    [Fact]
    public async Task Test49_ParkingSpot_PropertyOsApp_HardDeleteDenied()
    {
        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();

        var (companyA, buildingA) = await SeedCompanyAndBuilding(adminConn, "CompA", "BldgA");
        var spotA = await SeedParkingSpot(adminConn, companyA, buildingA, "A1");

        await using var appCmd = _appConnection!.CreateCommand();
        appCmd.CommandText = @"
            SELECT set_config('app.current_company_id', @cid, true);
            DELETE FROM parking_spots WHERE id = @spotId;
        ";
        appCmd.Parameters.AddWithValue("cid", companyA.ToString());
        appCmd.Parameters.AddWithValue("spotId", spotA);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => appCmd.ExecuteNonQueryAsync());
        ex.SqlState.Should().Be("42501"); // insufficient_privilege
    }
}
