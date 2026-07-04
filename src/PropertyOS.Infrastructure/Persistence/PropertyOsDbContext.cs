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
    // Module 2 — Subscriptions
    // ---------------------------------------------------------------------------

    public DbSet<PropertyOS.Domain.Subscriptions.SubscriptionPlan> SubscriptionPlans => Set<PropertyOS.Domain.Subscriptions.SubscriptionPlan>();
    public DbSet<PropertyOS.Domain.Subscriptions.CompanySubscription> CompanySubscriptions => Set<PropertyOS.Domain.Subscriptions.CompanySubscription>();

    // ---------------------------------------------------------------------------
    // Future modules will add their DbSets here as they are implemented.
    // Do not add DbSets speculatively — only add when the module is in scope.
    // ---------------------------------------------------------------------------

    // ---------------------------------------------------------------------------
    // Module 3 — Security / Identity / RBAC / Audit
    // ---------------------------------------------------------------------------

    public DbSet<PropertyOS.Domain.Identity.Entities.User> Users => Set<PropertyOS.Domain.Identity.Entities.User>();
    public DbSet<PropertyOS.Domain.Identity.Entities.UserCompanyRole> UserCompanyRoles => Set<PropertyOS.Domain.Identity.Entities.UserCompanyRole>();
    public DbSet<PropertyOS.Domain.Identity.Entities.Role> Roles => Set<PropertyOS.Domain.Identity.Entities.Role>();
    public DbSet<PropertyOS.Domain.Identity.Entities.Permission> Permissions => Set<PropertyOS.Domain.Identity.Entities.Permission>();
    public DbSet<PropertyOS.Domain.Identity.Entities.RolePermission> RolePermissions => Set<PropertyOS.Domain.Identity.Entities.RolePermission>();
    public DbSet<PropertyOS.Domain.Identity.Entities.RefreshToken> RefreshTokens => Set<PropertyOS.Domain.Identity.Entities.RefreshToken>();
    public DbSet<PropertyOS.Domain.Identity.Entities.LoginHistory> LoginHistory => Set<PropertyOS.Domain.Identity.Entities.LoginHistory>();
    public DbSet<PropertyOS.Domain.Identity.Entities.OtpChallenge> OtpChallenges => Set<PropertyOS.Domain.Identity.Entities.OtpChallenge>();
    public DbSet<PropertyOS.Domain.Audit.Entities.AuditLog> AuditLogs => Set<PropertyOS.Domain.Audit.Entities.AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Register PostgreSQL enums so EF Core maps them properly instead of as integers
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Companies.Enums.CompanyType>("company_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>("late_fee_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Subscriptions.Enums.SubscriptionStatusEnum>("subscription_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Subscriptions.Enums.BillingCycleEnum>("billing_cycle_enum");

        // Module 3
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Identity.Enums.MfaType>("mfa_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Identity.Enums.MembershipStatus>("membership_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Identity.Enums.RevokeReason>("revoke_reason_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Identity.Enums.LoginStatus>("login_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Identity.Enums.OtpPurpose>("otp_purpose_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Audit.Enums.AuditAction>("audit_action_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Audit.Enums.AuditSeverity>("audit_severity_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Audit.Enums.AuditSource>("audit_source_enum");

        // All entity configurations are discovered from IEntityTypeConfiguration<T>
        // classes in this assembly. This is the only call in OnModelCreating —
        // per the approved architecture, no configuration logic lives here.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropertyOsDbContext).Assembly);
    }
}
