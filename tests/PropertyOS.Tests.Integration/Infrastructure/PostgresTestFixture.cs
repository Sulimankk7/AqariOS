using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PropertyOS.Tests.Integration.Infrastructure;

public sealed class PostgresTestFixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres:17-alpine";

    private readonly PostgreSqlContainer _container;
    private NpgsqlDataSource? _dataSource;
    public NpgsqlDataSource? AppUserDataSource { get; private set; }

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

        // 1. Run migrations using a temporary schema-only context without MapEnum.
        // This ensures the database schema (including CREATE TYPE ... AS ENUM) is created first,
        // avoiding the chicken-and-egg problem where Npgsql caches the missing enums.
        var migrationOptions = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(_container.GetConnectionString())
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
                        GRANT ALL PRIVILEGES ON DATABASE propertyos_test TO app_user;
                        GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO app_user;
                        GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO app_user;
                    END IF;
                END $$;
            ");
        }

        // Build runtime connection string for non-RLS tests using the superuser
        var appConnectionString = _container.GetConnectionString();

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(appConnectionString);
        dataSourceBuilder.MapEnum<CompanyType>("company_type_enum");
        dataSourceBuilder.MapEnum<LateFeeType>("late_fee_type_enum");
        _dataSource = dataSourceBuilder.Build();

        // Build runtime connection string for app_user (for RLS tests)
        var appUserConnectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Username = "app_user",
            Password = "app_password"
        }.ToString();

        var appUserDataSourceBuilder = new NpgsqlDataSourceBuilder(appUserConnectionString);
        appUserDataSourceBuilder.MapEnum<CompanyType>("company_type_enum");
        appUserDataSourceBuilder.MapEnum<LateFeeType>("late_fee_type_enum");
        AppUserDataSource = appUserDataSourceBuilder.Build();

        // 3. Create the test Context backed by the mapped DataSource.
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseNpgsql(_dataSource, o =>
            {
                o.MapEnum<CompanyType>("company_type_enum");
                o.MapEnum<LateFeeType>("late_fee_type_enum");
            })
            .Options;

        Context = new PropertyOsDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var conn = await _dataSource!.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            TRUNCATE TABLE company_settings, companies
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
