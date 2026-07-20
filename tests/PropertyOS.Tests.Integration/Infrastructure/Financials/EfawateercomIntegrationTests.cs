using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.Infrastructure.Financials;

[Collection("Postgres collection")]
public class EfawateercomIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private Guid _companyId;
    private Guid _buildingId;
    private Guid _apartmentId;
    private Guid _tenantId;
    private Guid _rentPaymentId;
    private Guid _leaseContractId;

    public EfawateercomIntegrationTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        _companyId = Guid.NewGuid();
        _buildingId = Guid.NewGuid();
        _apartmentId = Guid.NewGuid();
        _tenantId = Guid.NewGuid();
        _rentPaymentId = Guid.NewGuid();
        _leaseContractId = Guid.NewGuid();
        var floorId = Guid.NewGuid();

        // Seed basic dependencies directly in PG
        await using var conn = ((NpgsqlConnection)_fixture.Context.Database.GetDbConnection()).CloneWith(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at) 
            VALUES (@cId, 'Integration Co', 'Integration', '+962790000000', 'individual_owner', now(), now());

            INSERT INTO buildings (id, company_id, name, building_type, total_floors, created_at, updated_at) 
            VALUES (@bId, @cId, 'Building 1', 'residential', 1, now(), now());

            INSERT INTO floors (id, company_id, building_id, floor_number, floor_label, floor_type, created_at, updated_at) 
            VALUES (@fId, @cId, @bId, 1, 'Floor 1', 'regular', now(), now());

            INSERT INTO apartments (id, floor_id, building_id, company_id, unit_number, occupancy_status, bedrooms, bathrooms, base_rent_amount, area_sqm, created_at, updated_at) 
            VALUES (@aId, @fId, @bId, @cId, '101', 'occupied', 2, 2, 500, 100, now(), now());

            INSERT INTO tenants (id, company_id, name, national_id, phone, created_at, updated_at) 
            VALUES (@tId, @cId, 'Tenant Name', '1234567890', '+962790000000', now(), now());

            INSERT INTO lease_contracts (id, company_id, tenant_id, building_id, apartment_id, contract_number, start_date, end_date, monthly_rent_amount, status, created_at, updated_at)
            VALUES (@lId, @cId, @tId, @bId, @aId, 'LC-1001', '2026-01-01', '2026-12-31', 500.00, 'active', now(), now());

            INSERT INTO rent_payments (id, company_id, lease_contract_id, tenant_id, building_id, apartment_id, payment_purpose, amount_due, amount_paid, currency, billing_period_start, billing_period_end, due_date, due_date_status, created_at, updated_at)
            VALUES (@rId, @cId, @lId, @tId, @bId, @aId, 'scheduled_installment', 500.00, 0.00, 'JOD', '2026-01-01', '2026-02-01', '2026-01-15', 'pending', now(), now());
        ";
        cmd.Parameters.Add(new NpgsqlParameter("cId", _companyId));
        cmd.Parameters.Add(new NpgsqlParameter("bId", _buildingId));
        cmd.Parameters.Add(new NpgsqlParameter("fId", floorId));
        cmd.Parameters.Add(new NpgsqlParameter("aId", _apartmentId));
        cmd.Parameters.Add(new NpgsqlParameter("tId", _tenantId));
        cmd.Parameters.Add(new NpgsqlParameter("lId", _leaseContractId));
        cmd.Parameters.Add(new NpgsqlParameter("rId", _rentPaymentId));
        await cmd.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CheckConstraint_AmountPositive_Enforced()
    {
        // Act & Assert
        // We bypass the C# domain validation using raw SQL to test that the PostgreSQL database constraint
        // "chk_efawateercom_transactions_amount_positive" (amount > 0) is genuinely active.
        await using var conn = new NpgsqlConnection(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO efawateercom_transactions (id, company_id, rent_payment_id, external_transaction_id, request_time, transaction_status, amount, currency, created_at, updated_at)
            VALUES (@id, @cId, @rId, 'TX-FAIL-1', now(), 'pending', -5.00, 'JOD', now(), now());
        ";
        cmd.Parameters.Add(new NpgsqlParameter("id", Guid.NewGuid()));
        cmd.Parameters.Add(new NpgsqlParameter("cId", _companyId));
        cmd.Parameters.Add(new NpgsqlParameter("rId", _rentPaymentId));

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Contains("chk_efawateercom_transactions_amount_positive", ex.ConstraintName);
    }

    [Fact]
    public async Task CheckConstraint_StatusConsistency_Enforced()
    {
        // Act & Assert
        // Test "chk_efawateercom_transactions_status_consistency" constraint:
        // A success/failed/timeout/cancelled transaction MUST have a non-null response_time.
        await using var conn = new NpgsqlConnection(_fixture.RawConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO efawateercom_transactions (id, company_id, rent_payment_id, external_transaction_id, request_time, transaction_status, response_time, amount, currency, created_at, updated_at)
            VALUES (@id, @cId, @rId, 'TX-FAIL-2', now(), 'success', NULL, 100.00, 'JOD', now(), now());
        ";
        cmd.Parameters.Add(new NpgsqlParameter("id", Guid.NewGuid()));
        cmd.Parameters.Add(new NpgsqlParameter("cId", _companyId));
        cmd.Parameters.Add(new NpgsqlParameter("rId", _rentPaymentId));

        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Contains("chk_efawateercom_transactions_status_consistency", ex.ConstraintName);
    }

    [Fact]
    public async Task UniqueIndex_ExternalTransactionId_Enforced_OnlyForActiveRecords()
    {
        // Act & Assert
        // We write raw SQL inserts to bypass domain aggregates and verify Postgres unique index constraint enforcement.
        await using var conn = new NpgsqlConnection(_fixture.RawConnectionString);
        await conn.OpenAsync();

        // First INSERT
        using (var cmd1 = conn.CreateCommand())
        {
            cmd1.CommandText = @"
                INSERT INTO efawateercom_transactions (id, company_id, rent_payment_id, external_transaction_id, request_time, transaction_status, amount, currency, created_at, updated_at)
                VALUES (@id, @cId, @rId, 'TX-DUP-100', now(), 'pending', 100.00, 'JOD', now(), now());
            ";
            cmd1.Parameters.Add(new NpgsqlParameter("id", Guid.NewGuid()));
            cmd1.Parameters.Add(new NpgsqlParameter("cId", _companyId));
            cmd1.Parameters.Add(new NpgsqlParameter("rId", _rentPaymentId));
            await cmd1.ExecuteNonQueryAsync();
        }

        // Second INSERT with identical external transaction ID (should throw UniqueViolation)
        using (var cmd2 = conn.CreateCommand())
        {
            cmd2.CommandText = @"
                INSERT INTO efawateercom_transactions (id, company_id, rent_payment_id, external_transaction_id, request_time, transaction_status, amount, currency, created_at, updated_at)
                VALUES (@id, @cId, @rId, 'TX-DUP-100', now(), 'pending', 200.00, 'JOD', now(), now());
            ";
            cmd2.Parameters.Add(new NpgsqlParameter("id", Guid.NewGuid()));
            cmd2.Parameters.Add(new NpgsqlParameter("cId", _companyId));
            cmd2.Parameters.Add(new NpgsqlParameter("rId", _rentPaymentId));

            var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd2.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
            Assert.Contains("uq_efawateercom_transactions_external_transaction_id", ex.ConstraintName);
        }
    }
}
