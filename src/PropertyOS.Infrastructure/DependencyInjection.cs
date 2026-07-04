using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Audit.Enums;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Infrastructure.Persistence.Audit;
using PropertyOS.Infrastructure.Persistence.Interceptors;

namespace PropertyOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Provide it via environment variables or a managed secret store.");

        // -----------------------------------------------------------------------
        // PostgreSQL enum mapping (Npgsql native enum support)
        // Every PostgreSQL enum used by Module 1 must be registered here via
        // NpgsqlDataSourceBuilder.MapEnum<T>() before the data source is built.
        //
        // Npgsql's DefaultNameTranslator maps C# PascalCase → approved snake_case:
        //   CompanyType.IndividualOwner           → 'individual_owner'
        //   CompanyType.PropertyManagementCompany → 'property_management_company'
        //   CompanyType.InvestmentCompany         → 'investment_company'
        //   LateFeeType.None                      → 'none'
        //   LateFeeType.Fixed                     → 'fixed'
        //   LateFeeType.Percentage                → 'percentage'
        // -----------------------------------------------------------------------
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

        dataSourceBuilder.MapEnum<CompanyType>(
            pgName: "company_type_enum",
            nameTranslator: null); // null = Npgsql DefaultNameTranslator (snake_case)

        dataSourceBuilder.MapEnum<LateFeeType>(
            pgName: "late_fee_type_enum",
            nameTranslator: null);

        // Module 3 Enums
        dataSourceBuilder.MapEnum<AuditAction>("audit_action_enum", null);
        dataSourceBuilder.MapEnum<AuditSeverity>("audit_severity_enum", null);
        dataSourceBuilder.MapEnum<AuditSource>("audit_source_enum", null);
        dataSourceBuilder.MapEnum<LoginStatus>("login_status_enum", null);
        dataSourceBuilder.MapEnum<MembershipStatus>("membership_status_enum", null);
        dataSourceBuilder.MapEnum<MfaType>("mfa_type_enum", null);
        dataSourceBuilder.MapEnum<OtpPurpose>("otp_purpose_enum", null);
        dataSourceBuilder.MapEnum<RevokeReason>("revoke_reason_enum", null);

        var dataSource = dataSourceBuilder.Build();

        // -----------------------------------------------------------------------
        // ITenantContext — pre-authentication placeholder (Module 1 boundary).
        // NullTenantContext is fail-closed: CompanyId returns null, causing
        // TenantSessionInterceptor to skip SET LOCAL and RLS to reject all
        // tenant-scoped queries.
        //
        // REPLACED in Module 3 by ClaimsPrincipalTenantContext, which reads
        // the authenticated JWT company_id claim from IHttpContextAccessor.
        // The DI registration below is replaced at that point — never retained
        // alongside the real implementation.
        // -----------------------------------------------------------------------
        services.AddScoped<ITenantContext, NullTenantContext>();
        services.AddScoped<ICurrentUserContext, NullCurrentUserContext>();


        // -----------------------------------------------------------------------
        // Register the TenantSessionInterceptor as a scoped service so EF Core
        // can inject ITenantContext per-request from DI.
        // -----------------------------------------------------------------------
        services.AddScoped<TenantSessionInterceptor>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddHttpContextAccessor();
        services.AddScoped<AuditTransactionInterceptor>();
        services.AddScoped<AuditTransactionState>();
        services.AddScoped<IAuditRequestContext, PropertyOS.Infrastructure.Audit.AuditRequestContext>();
        
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(PropertyOS.Infrastructure.Persistence.Behaviors.TransactionBehavior<,>));

        services.AddDbContext<PropertyOsDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(
                dataSource,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(PropertyOsDbContext).Assembly.FullName);

                    // EnableRetryOnFailure is safe for single-aggregate SaveChangesAsync
                    // commands and read-only queries (Architecture §12).
                    // NOT enabled for explicit-transaction orchestrators — those must
                    // disable retry individually.
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);

                    // Map enums for EF Core runtime type mapping
                    npgsqlOptions.MapEnum<CompanyType>("company_type_enum");
                    npgsqlOptions.MapEnum<LateFeeType>("late_fee_type_enum");

                    // Module 3
                    npgsqlOptions.MapEnum<AuditAction>("audit_action_enum");
                    npgsqlOptions.MapEnum<AuditSeverity>("audit_severity_enum");
                    npgsqlOptions.MapEnum<AuditSource>("audit_source_enum");
                    npgsqlOptions.MapEnum<LoginStatus>("login_status_enum");
                    npgsqlOptions.MapEnum<MembershipStatus>("membership_status_enum");
                    npgsqlOptions.MapEnum<MfaType>("mfa_type_enum");
                    npgsqlOptions.MapEnum<OtpPurpose>("otp_purpose_enum");
                    npgsqlOptions.MapEnum<RevokeReason>("revoke_reason_enum");
                });

            // Register the interceptor from the scoped DI container.
            // This is the approved pattern for injecting scoped services into
            // EF Core interceptors — using the serviceProvider overload.
            var tenantInterceptor = serviceProvider.GetRequiredService<TenantSessionInterceptor>();
            var auditInterceptor = serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>();
            var auditTxInterceptor = serviceProvider.GetRequiredService<AuditTransactionInterceptor>();
            options.AddInterceptors(tenantInterceptor, auditInterceptor, auditTxInterceptor);

            // Performance rules (Architecture §6):
            //   • Lazy loading disabled — EF Core does NOT enable lazy loading by default.
            //     No call needed — the default behavior is correct.
            //   • Sensitive data logging disabled in production.
            options.EnableSensitiveDataLogging(false);
        });

        return services;
    }

    private sealed class NullTenantContext : ITenantContext
    {
        public Guid? CompanyId => null;
        public bool IsPlatformAdmin => false;
    }

    private sealed class NullCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId => null;
    }
}
