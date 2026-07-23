using System;
using System.Threading.Tasks;
using FluentAssertions;
using Npgsql;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace PropertyOS.Tests.Integration.Rls;

[Collection("Postgres collection")]
public class DocumentsRlsTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private NpgsqlConnection? _appConnection;

    public DocumentsRlsTests(PostgresTestFixture fixture)
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

    [Fact]
    public async Task Rls_BuildingDocuments_ShouldIsolateBetweenTenants()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();

        await using var adminConn = new NpgsqlConnection(_fixture.RawConnectionString);
        await adminConn.OpenAsync();
        await using var cmd = adminConn.CreateCommand();

        // Seed Company A & B
        cmd.CommandText = $@"
            INSERT INTO companies (id, legal_name, display_name, primary_phone, company_type, created_at, updated_at, is_active)
            VALUES 
                ('{companyA}', 'Company A LLC', 'Company A', '+962790000001', 'individual_owner'::company_type_enum, now(), now(), true),
                ('{companyB}', 'Company B LLC', 'Company B', '+962790000002', 'individual_owner'::company_type_enum, now(), now(), true);

            INSERT INTO document_categories (id, company_id, name)
            VALUES ('{Guid.NewGuid()}', '{companyA}', 'Insurance Company A');
        ";
        await cmd.ExecuteNonQueryAsync();

        // Query as Tenant A via restricted app user connection
        await using var txA = await _appConnection!.BeginTransactionAsync();
        await using var setTenantCmd = _appConnection.CreateCommand();
        setTenantCmd.CommandText = $"SET LOCAL app.current_company_id = '{companyA}';";
        setTenantCmd.Transaction = txA;
        await setTenantCmd.ExecuteNonQueryAsync();

        await using var queryCmd = _appConnection.CreateCommand();
        queryCmd.CommandText = "SELECT COUNT(*) FROM document_categories;";
        queryCmd.Transaction = txA;
        var countTenantA = Convert.ToInt32(await queryCmd.ExecuteScalarAsync());
        await txA.RollbackAsync();

        // Switch session to Tenant B
        await using var txB = await _appConnection.BeginTransactionAsync();
        await using var setTenantBCmd = _appConnection.CreateCommand();
        setTenantBCmd.CommandText = $"SET LOCAL app.current_company_id = '{companyB}';";
        setTenantBCmd.Transaction = txB;
        await setTenantBCmd.ExecuteNonQueryAsync();

        await using var queryBCmd = _appConnection.CreateCommand();
        queryBCmd.CommandText = "SELECT COUNT(*) FROM document_categories;";
        queryBCmd.Transaction = txB;
        var countTenantB = Convert.ToInt32(await queryBCmd.ExecuteScalarAsync());
        await txB.RollbackAsync();

        countTenantA.Should().Be(1);
        countTenantB.Should().Be(0); // RLS strictly blocks Company B from seeing Company A's categories
    }
}
