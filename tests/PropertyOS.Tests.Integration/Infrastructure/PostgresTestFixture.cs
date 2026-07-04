using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Domain.Audit.Enums;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Domain.Subscriptions.Enums;
using PropertyOS.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PropertyOS.Tests.Integration.Infrastructure;

public sealed class PostgresTestFixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres:17-alpine";

    private readonly PostgreSqlContainer _container;
    private NpgsqlDataSource? _dataSource;
    public NpgsqlDataSource? AppUserDataSource { get; private set; }
    public string RawConnectionString { get; private set; } = string.Empty;
    public string AppUserConnectionString { get; private set; } = string.Empty;

    public PropertyOsDbContext Context { get; private set; } = null!;

    public PostgresTestFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .WithDatabase("propertyos_test")
            .WithUsername("propertyos")
            .WithPassword("propertyos_pass")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        RawConnectionString = _container.GetConnectionString();

        // 1. Run migrations using a temporary schema-only context without MapEnum.
        // This ensures the database schema (including CREATE TYPE ... AS ENUM) is created first,
        // avoiding the chicken-and-egg problem where Npgsql caches the missing enums.
        var connString = _container.GetConnectionString();
        await using (var conn = new NpgsqlConnection(connString))
        {
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'propertyos_owner') THEN
                        CREATE ROLE propertyos_owner NOLOGIN;
                    END IF;
                    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'propertyos_app') THEN
                        CREATE ROLE propertyos_app LOGIN PASSWORD 'test_password';
                    END IF;
                    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'propertyos_auth') THEN
                        CREATE ROLE propertyos_auth LOGIN PASSWORD 'test_password';
                    END IF;
                END $$;
            ", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        var migrationOptions = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(connString)
            .Options;

        await using (var migrationContext = new PropertyOsDbContext(migrationOptions))
        {


            await migrationContext.Database.MigrateAsync();
            
            // Create a non-superuser role for runtime tests to ensure RLS is genuinely enforced.
            // (A PostgreSQL superuser cannot remove its own superuser status).
            await migrationContext.Database.ExecuteSqlRawAsync(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'app_user') THEN
                        CREATE ROLE app_user WITH LOGIN PASSWORD 'app_password' NOSUPERUSER NOCREATEDB NOCREATEROLE;
                        GRANT propertyos_app TO app_user; -- grant the app role to the test user
                    END IF;
                END $$;
            ");
        }

        // Build runtime connection string for non-RLS tests using the superuser
        var appConnectionString = _container.GetConnectionString();

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(appConnectionString);
        dataSourceBuilder.MapEnum<CompanyType>("company_type_enum");
        dataSourceBuilder.MapEnum<LateFeeType>("late_fee_type_enum");
        dataSourceBuilder.MapEnum<SubscriptionStatusEnum>("subscription_status_enum");
        dataSourceBuilder.MapEnum<BillingCycleEnum>("billing_cycle_enum");
        // Module 3
        dataSourceBuilder.MapEnum<AuditAction>("audit_action_enum");
        dataSourceBuilder.MapEnum<AuditSeverity>("audit_severity_enum");
        dataSourceBuilder.MapEnum<AuditSource>("audit_source_enum");
        dataSourceBuilder.MapEnum<LoginStatus>("login_status_enum");
        dataSourceBuilder.MapEnum<MembershipStatus>("membership_status_enum");
        dataSourceBuilder.MapEnum<MfaType>("mfa_type_enum");
        dataSourceBuilder.MapEnum<OtpPurpose>("otp_purpose_enum");
        dataSourceBuilder.MapEnum<RevokeReason>("revoke_reason_enum");
        // Module 4
        dataSourceBuilder.MapEnum<BuildingType>("building_type_enum");
        dataSourceBuilder.MapEnum<Governorate>("governorate_enum");
        dataSourceBuilder.MapEnum<FloorType>("floor_type_enum");
        dataSourceBuilder.MapEnum<OwnershipStatus>("ownership_status_enum");
        dataSourceBuilder.MapEnum<OccupancyStatus>("occupancy_status_enum");
        _dataSource = dataSourceBuilder.Build();

        AppUserConnectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Username = "app_user",
            Password = "app_password"
        }.ToString();

        var appUserConnectionString = AppUserConnectionString;

        var appUserDataSourceBuilder = new NpgsqlDataSourceBuilder(appUserConnectionString);
        appUserDataSourceBuilder.MapEnum<CompanyType>("company_type_enum");
        appUserDataSourceBuilder.MapEnum<LateFeeType>("late_fee_type_enum");
        appUserDataSourceBuilder.MapEnum<SubscriptionStatusEnum>("subscription_status_enum");
        appUserDataSourceBuilder.MapEnum<BillingCycleEnum>("billing_cycle_enum");
        // Module 3
        appUserDataSourceBuilder.MapEnum<AuditAction>("audit_action_enum");
        appUserDataSourceBuilder.MapEnum<AuditSeverity>("audit_severity_enum");
        appUserDataSourceBuilder.MapEnum<AuditSource>("audit_source_enum");
        appUserDataSourceBuilder.MapEnum<LoginStatus>("login_status_enum");
        appUserDataSourceBuilder.MapEnum<MembershipStatus>("membership_status_enum");
        appUserDataSourceBuilder.MapEnum<MfaType>("mfa_type_enum");
        appUserDataSourceBuilder.MapEnum<OtpPurpose>("otp_purpose_enum");
        appUserDataSourceBuilder.MapEnum<RevokeReason>("revoke_reason_enum");
        // Module 4
        appUserDataSourceBuilder.MapEnum<BuildingType>("building_type_enum");
        appUserDataSourceBuilder.MapEnum<Governorate>("governorate_enum");
        appUserDataSourceBuilder.MapEnum<FloorType>("floor_type_enum");
        appUserDataSourceBuilder.MapEnum<OwnershipStatus>("ownership_status_enum");
        appUserDataSourceBuilder.MapEnum<OccupancyStatus>("occupancy_status_enum");
        AppUserDataSource = appUserDataSourceBuilder.Build();

        // 3. Create the test Context backed by the mapped DataSource.
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(_dataSource, o =>
            {
                o.MapEnum<CompanyType>("company_type_enum");
                o.MapEnum<LateFeeType>("late_fee_type_enum");
                o.MapEnum<SubscriptionStatusEnum>("subscription_status_enum");
                o.MapEnum<BillingCycleEnum>("billing_cycle_enum");
                // Module 3
                o.MapEnum<AuditAction>("audit_action_enum");
                o.MapEnum<AuditSeverity>("audit_severity_enum");
                o.MapEnum<AuditSource>("audit_source_enum");
                o.MapEnum<LoginStatus>("login_status_enum");
                o.MapEnum<MembershipStatus>("membership_status_enum");
                o.MapEnum<MfaType>("mfa_type_enum");
                o.MapEnum<OtpPurpose>("otp_purpose_enum");
                o.MapEnum<RevokeReason>("revoke_reason_enum");
                // Module 4
                o.MapEnum<BuildingType>("building_type_enum");
                o.MapEnum<Governorate>("governorate_enum");
                o.MapEnum<FloorType>("floor_type_enum");
                o.MapEnum<OwnershipStatus>("ownership_status_enum");
                o.MapEnum<OccupancyStatus>("occupancy_status_enum");
            })
            .Options;

        Context = new PropertyOsDbContext(options);

        // --- VERIFY SECURITY DEFINER ---
        await using var verifyConn = await _dataSource!.OpenConnectionAsync();
        await using var verifyCmd = verifyConn.CreateCommand();
        verifyCmd.CommandText = @"
            SELECT 
                r.rolname as owner,
                has_function_privilege('propertyos_app', 'insert_audit_log(character varying, uuid, audit_action_enum, jsonb, jsonb, timestamp with time zone, uuid, uuid, uuid, uuid, inet, text, audit_severity_enum, audit_source_enum, jsonb)', 'execute') as app_can_execute,
                has_function_privilege('propertyos_auth', 'insert_audit_log(character varying, uuid, audit_action_enum, jsonb, jsonb, timestamp with time zone, uuid, uuid, uuid, uuid, inet, text, audit_severity_enum, audit_source_enum, jsonb)', 'execute') as auth_can_execute,
                has_function_privilege('public', 'insert_audit_log(character varying, uuid, audit_action_enum, jsonb, jsonb, timestamp with time zone, uuid, uuid, uuid, uuid, inet, text, audit_severity_enum, audit_source_enum, jsonb)', 'execute') as public_can_execute
            FROM pg_proc p
            JOIN pg_roles r ON p.proowner = r.oid
            WHERE p.proname = 'insert_audit_log';
        ";
        await using var reader = await verifyCmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            Console.WriteLine($"[SECURITY VERIFICATION] insert_audit_log owner = {reader.GetString(0)}");
            Console.WriteLine($"[SECURITY VERIFICATION] propertyos_app has EXECUTE = {reader.GetBoolean(1)}");
            Console.WriteLine($"[SECURITY VERIFICATION] propertyos_auth has EXECUTE = {reader.GetBoolean(2)}");
            Console.WriteLine($"[SECURITY VERIFICATION] PUBLIC has EXECUTE = {reader.GetBoolean(3)}");
            if (reader.GetString(0) != "propertyos_owner") throw new Exception("Owner must be propertyos_owner");
            if (!reader.GetBoolean(1)) throw new Exception("propertyos_app must have EXECUTE");
            if (!reader.GetBoolean(2)) throw new Exception("propertyos_auth must have EXECUTE");
            if (reader.GetBoolean(3)) throw new Exception("PUBLIC must NOT have EXECUTE");
        }
        else
        {
            throw new Exception("Function insert_audit_log not found");
        }
        // -------------------------------
    }

    public async Task ResetDatabaseAsync()
    {
        await using var conn = await _dataSource!.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            TRUNCATE TABLE apartments, building_addresses, floors, buildings,
                           company_subscriptions, subscription_plans, company_settings, companies
            RESTART IDENTITY
            CASCADE;
        ";

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (Context is not null)
            await Context.DisposeAsync();
        if (_dataSource is not null)
            await _dataSource.DisposeAsync();
        if (AppUserDataSource is not null)
            await AppUserDataSource.DisposeAsync();
        await _container.DisposeAsync();
    }
}
