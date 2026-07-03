using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using PropertyOS.Domain.Companies.Enums;

namespace PropertyOS.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core tooling (dotnet ef migrations).
/// 
/// This class is used ONLY at design time — never at runtime.
/// It provides a fully configured PropertyOsDbContext instance to the EF Core
/// CLI tools without requiring the full application host (Program.cs) to start.
///
/// IMPORTANT:
///   • This class must remain in the Infrastructure project alongside the DbContext.
///   • It should never be registered in DI (it's only used by EF Core tooling).
///   • The connection string here can be a placeholder/local value — migrations are
///     only generated locally; they are applied against the real database separately.
///   • PostgreSQL enum mappings must be registered here too, as the tooling does
///     not go through AddInfrastructureServices().
/// </summary>
internal sealed class PropertyOsDbContextFactory : IDesignTimeDbContextFactory<PropertyOsDbContext>
{
    public PropertyOsDbContext CreateDbContext(string[] args)
    {
        // Design-time connection string — used only by EF Core tooling for
        // migration generation and script output, never for production access.
        // Falls back to an environment variable if set, otherwise uses a local dev value.
        var connectionString = Environment.GetEnvironmentVariable("PROPERTYOS_DB")
            ?? "Host=localhost;Port=5432;Database=propertyos_dev;Username=postgres;Password=postgres";

        // Register the same PostgreSQL enum mappings as AddInfrastructureServices().
        // EF Core tooling bypasses the DI pipeline, so we must register them here
        // independently to ensure the EF Core model is consistent with the runtime model.
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

        dataSourceBuilder.MapEnum<CompanyType>(
            pgName: "company_type_enum",
            nameTranslator: null);

        dataSourceBuilder.MapEnum<LateFeeType>(
            pgName: "late_fee_type_enum",
            nameTranslator: null);

        var dataSource = dataSourceBuilder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<PropertyOsDbContext>();
        optionsBuilder.UseNpgsql(
            dataSource,
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(PropertyOsDbContext).Assembly.FullName);
            });

        return new PropertyOsDbContext(optionsBuilder.Options);
    }
}
