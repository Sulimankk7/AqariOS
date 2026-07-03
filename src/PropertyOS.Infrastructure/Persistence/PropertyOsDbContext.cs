using Microsoft.EntityFrameworkCore;
using PropertyOS.Domain.Companies;

namespace PropertyOS.Infrastructure.Persistence;

/// <summary>
/// The single EF Core DbContext for PropertyOS.
/// 
/// RESPONSIBILITIES:
///   • Exposes DbSet properties for each aggregate root / entity (added per module).
///   • Delegates all entity configuration to IEntityTypeConfiguration classes in
///     each module's Infrastructure/&lt;Module&gt;/Configurations/ folder.
///   • Calls ApplyConfigurationsFromAssembly to discover all configurations automatically.
///
/// STRICT RULES (per approved architecture):
///   • No business logic here.
///   • No EnsureCreated / EnsureDeleted / Migrate calls.
///   • No lazy loading proxies.
///   • New DbSets are added only as each module is implemented.
/// </summary>
public class PropertyOsDbContext : DbContext
{
    public PropertyOsDbContext(DbContextOptions<PropertyOsDbContext> options)
        : base(options)
    {
    }

    // ---------------------------------------------------------------------------
    // Module 1 — Core
    // ---------------------------------------------------------------------------

    /// <summary>Tenant root entity. Every other tenant-scoped table traces back here.</summary>
    public DbSet<Company> Companies => Set<Company>();

    /// <summary>One-to-one configuration extension of Company.</summary>
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();

    // ---------------------------------------------------------------------------
    // Future modules will add their DbSets here as they are implemented.
    // Do not add DbSets speculatively — only add when the module is in scope.
    // ---------------------------------------------------------------------------

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Register PostgreSQL enums so EF Core maps them properly instead of as integers
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Companies.Enums.CompanyType>("company_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>("late_fee_type_enum");

        // All entity configurations are discovered from IEntityTypeConfiguration<T>
        // classes in this assembly. This is the only call in OnModelCreating —
        // per the approved architecture, no configuration logic lives here.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropertyOsDbContext).Assembly);
    }
}
