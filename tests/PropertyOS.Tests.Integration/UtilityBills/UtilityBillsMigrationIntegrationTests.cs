using FluentAssertions;
using Npgsql;
using PropertyOS.Tests.Integration.Infrastructure;
using Xunit;

namespace PropertyOS.Tests.Integration.UtilityBills;

[Collection("Postgres collection")]
public sealed class UtilityBillsMigrationIntegrationTests
{
    private readonly PostgresTestFixture _fixture;

    public UtilityBillsMigrationIntegrationTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migrations_FromEmpty_CreateCanonicalUtilityBillsSchemaAndSecurity()
    {
        await using var connection = new NpgsqlConnection(_fixture.RawConnectionString);
        await connection.OpenAsync();

        (await ScalarAsync<long>(connection, """
            SELECT COUNT(*)
            FROM "__EFMigrationsHistory"
            WHERE "MigrationId" = '20260820120000_Module12_UtilityBills'
            """)).Should().Be(1);
        (await ScalarAsync<long>(connection, """
            SELECT COUNT(*)
            FROM "__EFMigrationsHistory"
            WHERE "MigrationId" = '20260825120000_AddUtilityInvalidAccountSyncStatus'
            """)).Should().Be(1);

        (await QueryStringsAsync(connection, """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name IN ('utility_accounts', 'utility_bills')
            ORDER BY table_name
            """)).Should().Equal("utility_accounts", "utility_bills");

        (await QueryStringsAsync(connection, """
            SELECT t.typname || ':' || e.enumlabel
            FROM pg_type t
            JOIN pg_enum e ON e.enumtypid = t.oid
            WHERE t.typname IN (
                'utility_type_enum',
                'utility_sync_status_enum',
                'utility_bill_status_enum')
            ORDER BY t.typname, e.enumsortorder
            """)).Should().Equal(
                "utility_bill_status_enum:unpaid",
                "utility_bill_status_enum:paid",
                "utility_bill_status_enum:unknown",
                "utility_sync_status_enum:never_synced",
                "utility_sync_status_enum:syncing",
                "utility_sync_status_enum:synced",
                "utility_sync_status_enum:provider_error",
                "utility_sync_status_enum:rate_limited",
                "utility_sync_status_enum:timeout",
                "utility_sync_status_enum:suspended",
                "utility_sync_status_enum:invalid_account",
                "utility_type_enum:electricity",
                "utility_type_enum:water");

        (await QueryStringsAsync(connection, """
            SELECT enumlabel
            FROM pg_type t
            JOIN pg_enum e ON e.enumtypid = t.oid
            WHERE t.typname = 'notification_type_enum'
              AND enumlabel IN ('utility_bill_electricity', 'utility_bill_water')
            ORDER BY enumlabel
            """)).Should().Equal("utility_bill_electricity", "utility_bill_water");

        (await QueryStringsAsync(connection, """
            SELECT conname
            FROM pg_constraint
            WHERE conrelid IN ('utility_accounts'::regclass, 'utility_bills'::regclass)
            ORDER BY conname
            """)).Should().Contain(new[]
            {
                "pk_utility_accounts",
                "pk_utility_bills",
                "fk_utility_accounts_companies_company_id",
                "fk_utility_accounts_lease_contracts_lease_contract_id",
                "fk_utility_accounts_tenants_tenant_id",
                "fk_utility_accounts_apartments_apartment_id",
                "fk_utility_bills_companies_company_id",
                "fk_utility_bills_utility_accounts_utility_account_id",
                "chk_utility_accounts_account_number_not_blank",
                "chk_utility_accounts_interval_consistent",
                "chk_utility_accounts_claim_fields_consistent",
                "chk_utility_bills_amount_positive",
                "chk_utility_bills_currency_length",
                "chk_utility_bills_notification_after_discovered"
            });

        (await QueryStringsAsync(connection, """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND tablename IN ('utility_accounts', 'utility_bills')
            ORDER BY indexname
            """)).Should().Contain(new[]
            {
                "uq_utility_accounts_lease_type",
                "uq_utility_accounts_type_number",
                "idx_utility_accounts_scheduler",
                "idx_utility_accounts_bootstrap_pending",
                "idx_utility_accounts_company_tenant",
                "idx_utility_accounts_lease",
                "uq_utility_bills_account_external_id",
                "idx_utility_bills_account_date",
                "idx_utility_bills_company_date",
                "idx_utility_bills_notification_pending"
            });

        (await QueryStringsAsync(connection, """
            SELECT relname || ':' || relrowsecurity || ':' || relforcerowsecurity
            FROM pg_class
            WHERE relname IN ('utility_accounts', 'utility_bills')
            ORDER BY relname
            """)).Should().Equal(
                "utility_accounts:true:true",
                "utility_bills:true:true");

        (await QueryStringsAsync(connection, """
            SELECT policyname
            FROM pg_policies
            WHERE schemaname = 'public'
              AND tablename IN ('utility_accounts', 'utility_bills')
            ORDER BY policyname
            """)).Should().Equal(
                "utility_accounts_tenant_isolation_policy",
                "utility_bills_tenant_isolation_policy");

        (await ScalarAsync<bool>(connection, """
            SELECT has_table_privilege('propertyos_app', 'utility_accounts', 'SELECT,INSERT,UPDATE,DELETE')
               AND has_table_privilege('propertyos_app', 'utility_bills', 'SELECT,INSERT,UPDATE,DELETE')
            """)).Should().BeTrue();
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<List<string>> QueryStringsAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
            values.Add(reader.GetString(0));
        return values;
    }
}
