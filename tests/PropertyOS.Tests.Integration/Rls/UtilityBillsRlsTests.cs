using System;
using System.Threading.Tasks;
using FluentAssertions;
using Npgsql;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Rls;

/// <summary>
/// Module 12 RLS: standard tenant isolation on utility_accounts and utility_bills.
/// </summary>
[Collection("Postgres collection")]
public class UtilityBillsRlsTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private NpgsqlConnection? _appConnection;

    private readonly Guid _companyA = Guid.NewGuid();
    private readonly Guid _companyB = Guid.NewGuid();
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _leaseA = Guid.NewGuid();
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _apartmentA = Guid.NewGuid();
    private readonly Guid _buildingA = Guid.NewGuid();
    private readonly Guid _accountA = Guid.NewGuid();

    public UtilityBillsRlsTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _appConnection = await _fixture.AppUserDataSource!.OpenConnectionAsync();

        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();
        await using var cmd = adminConn.CreateCommand();
        cmd.CommandText = $@"
            INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at, is_active)
            VALUES
                ('{_companyA}', 'Company A LLC', 'Company A', '+962790000001', 'individual_owner'::company_type_enum, now(), now(), true),
                ('{_companyB}', 'Company B LLC', 'Company B', '+962790000002', 'individual_owner'::company_type_enum, now(), now(), true);

            INSERT INTO users (id, full_name, email)
            VALUES ('{_userA}', 'User A', 'usera@example.com');

            INSERT INTO buildings (id, company_id, name, building_type, total_floors, created_at, updated_at)
            VALUES ('{_buildingA}', '{_companyA}', 'Building 1', 'residential'::building_type_enum, 4, now(), now());

            INSERT INTO floors (id, company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at)
            VALUES ('{Guid.NewGuid()}', '{_companyA}', '{_buildingA}', 1, 'Floor 1', 'regular'::floor_type_enum, now(), now());

            INSERT INTO apartments (id, floor_id, building_id, company_id, unit_number, occupancy_status, bedrooms, bathrooms, base_rent_amount, area_sqm, created_at, updated_at)
            VALUES ('{_apartmentA}', (SELECT id FROM floors WHERE building_id = '{_buildingA}' LIMIT 1), '{_buildingA}', '{_companyA}', '101', 'vacant'::occupancy_status_enum, 2, 1, 400.0, 90.0, now(), now());

            INSERT INTO tenants (id, company_id, name, national_id, phone, user_id, created_at, updated_at)
            VALUES ('{_tenantA}', '{_companyA}', 'Tenant A', '9990001111', '+962791112233', '{_userA}', now(), now());

            INSERT INTO lease_contracts (id, company_id, building_id, apartment_id, tenant_id, contract_number, start_date, end_date, monthly_rent_amount, payment_frequency, payment_due_day, created_at, updated_at)
            VALUES ('{_leaseA}', '{_companyA}', '{_buildingA}', '{_apartmentA}', '{_tenantA}', 'LC-RLS-001', '2026-01-01', '2026-12-31', 400.0, 'monthly'::payment_frequency_enum, 1, now(), now());


            INSERT INTO utility_accounts (id, company_id, lease_contract_id, tenant_id, apartment_id, utility_type, account_number, is_active, historical_bootstrap_completed)
            VALUES ('{_accountA}', '{_companyA}', '{_leaseA}', '{_tenantA}', '{_apartmentA}', 'electricity'::utility_type_enum, 'ELEC-RLS-100', true, true);

            INSERT INTO utility_bills (id, company_id, utility_account_id, utility_type, provider_external_id, bill_date, amount, currency, is_paid)
            VALUES ('{Guid.NewGuid()}', '{_companyA}', '{_accountA}', 'electricity'::utility_type_enum, 'BILL-RLS-01', '2026-05-01', 55.0, 'JOD', false);
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (_appConnection != null)
            await _appConnection.DisposeAsync();
    }

    private async Task<int> CountAsync(string sql, Guid companyId, bool isPlatformAdmin = false)
    {
        await using var tx = await _appConnection!.BeginTransactionAsync();
        await using var setCmd = _appConnection.CreateCommand();
        setCmd.CommandText = $"SET LOCAL app.current_company_id = '{companyId}';";
        if (isPlatformAdmin)
            setCmd.CommandText += " SET LOCAL app.is_platform_admin = 'true';";
        setCmd.Transaction = tx;
        await setCmd.ExecuteNonQueryAsync();

        await using var queryCmd = _appConnection.CreateCommand();
        queryCmd.CommandText = sql;
        queryCmd.Transaction = tx;
        var count = Convert.ToInt32(await queryCmd.ExecuteScalarAsync());
        await tx.RollbackAsync();
        return count;
    }

    [Fact]
    public async Task Rls_UtilityAccounts_CompanyA_CanSeeOwnAccounts()
    {
        (await CountAsync("SELECT COUNT(*) FROM utility_accounts;", _companyA)).Should().Be(1);
    }

    [Fact]
    public async Task Rls_UtilityAccounts_CompanyB_CannotSeeCompanyAAccounts()
    {
        (await CountAsync("SELECT COUNT(*) FROM utility_accounts;", _companyB)).Should().Be(0);
    }

    [Fact]
    public async Task Rls_UtilityBills_CompanyA_CanSeeOwnBills()
    {
        (await CountAsync("SELECT COUNT(*) FROM utility_bills;", _companyA)).Should().Be(1);
    }

    [Fact]
    public async Task Rls_UtilityBills_CompanyB_CannotSeeCompanyABills()
    {
        (await CountAsync("SELECT COUNT(*) FROM utility_bills;", _companyB)).Should().Be(0);
    }

    [Fact]
    public async Task Rls_UtilityAccounts_PlatformAdmin_CanSeeAll()
    {
        (await CountAsync("SELECT COUNT(*) FROM utility_accounts;", _companyB, isPlatformAdmin: true)).Should().Be(1);
    }
}
