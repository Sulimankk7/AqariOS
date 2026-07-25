using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Audit.Enums;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Domain.Properties.Enums;
using PropertyOS.Infrastructure.Persistence;
using PropertyOS.Infrastructure.Persistence.Audit;
using PropertyOS.Infrastructure.Persistence.Interceptors;
using PropertyOS.Application.Leasing;
using PropertyOS.Infrastructure.Leasing.Repositories;
using PropertyOS.Application.Financials;
using PropertyOS.Infrastructure.Financials.Repositories;
using PropertyOS.Application.Maintenance;
using PropertyOS.Infrastructure.Maintenance.Repositories;
using PropertyOS.Domain.Maintenance.Enums;
using PropertyOS.Application.Marketplace;
using PropertyOS.Infrastructure.Marketplace.Repositories;
using PropertyOS.Infrastructure.Marketplace.Services;
using PropertyOS.Domain.Common.Enums;
using PropertyOS.Domain.Marketplace.Enums;
using PropertyOS.Application.Properties;
using PropertyOS.Infrastructure.Properties.Repositories;


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

        // Module 4 enums
        dataSourceBuilder.MapEnum<BuildingType>("building_type_enum", null);
        dataSourceBuilder.MapEnum<Governorate>("governorate_enum", null);
        dataSourceBuilder.MapEnum<FloorType>("floor_type_enum", null);
        dataSourceBuilder.MapEnum<OwnershipStatus>("ownership_status_enum", null);
        dataSourceBuilder.MapEnum<OccupancyStatus>("occupancy_status_enum", null);
        dataSourceBuilder.MapEnum<ParkingType>("parking_type_enum", null);
        dataSourceBuilder.MapEnum<ParkingAssignmentStatus>("parking_assignment_status_enum", null);

        // Module 5 enums
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractStatus>("contract_status_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.PaymentFrequency>("payment_frequency_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.TerminationType>("termination_type_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractDocumentType>("contract_document_type_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.LegalRegime>("legal_regime_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.TenantType>("tenant_type_enum", null);

        // Module 6 enums
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentPurpose>("payment_purpose_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentMethod>("payment_method_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.DueDateStatus>("due_date_status_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ChequeStatus>("cheque_status_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.AllocationStatus>("allocation_status_enum", null);

        // Module 7 enums
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ExpenseCategory>("expense_category_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ExpensePaymentMethod>("expense_payment_method_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ReceiptResetPolicy>("receipt_reset_policy_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.EfawateercomStatus>("efawateercom_status_enum", null);

        // Module 8 enums
        dataSourceBuilder.MapEnum<MaintenanceCategory>("maintenance_category_enum", null);
        dataSourceBuilder.MapEnum<MaintenancePriority>("maintenance_priority_enum", null);
        dataSourceBuilder.MapEnum<MaintenanceStatus>("maintenance_status_enum", null);

        // Module 9 enums
        dataSourceBuilder.MapEnum<CurrencyCode>("currency_code_enum", null);
        dataSourceBuilder.MapEnum<ListingStatus>("listing_status_enum", null);
        dataSourceBuilder.MapEnum<ViewingRequestStatus>("viewing_request_status_enum", null);

        // Module 11 enums
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Notifications.Enums.NotificationType>("notification_type_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Notifications.Enums.NotificationStatus>("notification_status_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Notifications.Enums.NotificationPriority>("notification_priority_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Notifications.Enums.DeliveryChannel>("delivery_channel_enum", null);
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Notifications.Enums.DeliveryStatus>("delivery_status_enum", null);

        var dataSource = dataSourceBuilder.Build();

        // -----------------------------------------------------------------------
        // Module 3 — Security / Identity / Auth Context Providers & Services
        // -----------------------------------------------------------------------
        services.AddScoped<ITenantContext, PropertyOS.Infrastructure.Identity.ClaimsPrincipalTenantContext>();
        services.AddScoped<ICurrentUserContext, PropertyOS.Infrastructure.Identity.ClaimsPrincipalCurrentUserContext>();
        services.AddScoped<PropertyOS.Application.Identity.IPasswordHasher, PropertyOS.Infrastructure.Identity.PasswordHasher>();
        services.AddScoped<PropertyOS.Application.Identity.IJwtTokenGenerator, PropertyOS.Infrastructure.Identity.JwtTokenGenerator>();
        services.AddScoped<PropertyOS.Application.Identity.IAuthService, PropertyOS.Infrastructure.Identity.AuthService>();
        services.AddScoped<PropertyOS.Application.Common.Interfaces.IPermissionCatalogSeeder, PropertyOS.Infrastructure.Identity.PermissionCatalogSeeder>();
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<PropertyOsDbContext>());


        // -----------------------------------------------------------------------
        // Register the TenantSessionInterceptor as a scoped service so EF Core
        // can inject ITenantContext per-request from DI.
        // -----------------------------------------------------------------------
        services.AddScoped<TenantSessionInterceptor>();
        services.AddScoped<PropertyOS.Application.Companies.ICompanyRepository, PropertyOS.Infrastructure.Companies.Repositories.CompanyRepository>();
        services.AddScoped<ILeaseContractRepository, LeaseContractRepository>();
        services.AddScoped<ILeasingReferenceRepository, LeasingReferenceRepository>();
        services.AddScoped<IRentPaymentRepository, RentPaymentRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICompanyReceiptSequenceRepository, CompanyReceiptSequenceRepository>();
        services.AddScoped<IEfawateercomTransactionRepository, EfawateercomTransactionRepository>();
        services.AddScoped<IEfawateercomGateway, PropertyOS.Infrastructure.Payments.NullEfawateercomGateway>();
        services.AddScoped<IMaintenanceRequestRepository, MaintenanceRequestRepository>();
        services.AddScoped<IMaintenanceQueries, MaintenanceQueries>();

        // Module 9 - Marketplace
        services.AddScoped<IMarketplaceListingRepository, MarketplaceListingRepository>();
        services.AddScoped<IViewingRequestRepository, ViewingRequestRepository>();
        services.AddScoped<IMarketplaceQueries, MarketplaceQueries>();
        services.AddScoped<IFileStorageValidator, FileStorageValidator>();

        // Module 10 - Documents & Files Subsystem
        services.Configure<PropertyOS.Application.Files.Options.FileStorageOptions>(configuration.GetSection(PropertyOS.Application.Files.Options.FileStorageOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PropertyOS.Application.Files.Options.FileStorageOptions>>().Value);
        services.AddScoped<PropertyOS.Application.Files.IFileStorageRepository, PropertyOS.Infrastructure.Files.Repositories.FileStorageRepository>();
        services.AddScoped<PropertyOS.Application.Files.Services.IStorageProvider, PropertyOS.Infrastructure.Files.Services.PhysicalFileStorageProvider>();
        services.AddScoped<PropertyOS.Application.Documents.IDocumentCategoryRepository, PropertyOS.Infrastructure.Documents.Repositories.DocumentCategoryRepository>();
        services.AddScoped<PropertyOS.Application.Documents.IBuildingDocumentRepository, PropertyOS.Infrastructure.Documents.Repositories.BuildingDocumentRepository>();
        services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PropertyOS.Infrastructure.Documents.Security.ConfidentialDocumentAuthorizationHandler>();

        // Module 4 - Properties
        services.AddScoped<IBuildingRepository, BuildingRepository>();
        services.AddScoped<IFloorRepository, FloorRepository>();
        services.AddScoped<IApartmentRepository, ApartmentRepository>();
        services.AddScoped<IParkingSpotRepository, ParkingSpotRepository>();

        // Module 11 - Notifications
        services.AddScoped<PropertyOS.Application.Notifications.INotificationTemplateRepository, PropertyOS.Infrastructure.Notifications.Repositories.NotificationTemplateRepository>();
        services.AddScoped<PropertyOS.Application.Notifications.INotificationRepository, PropertyOS.Infrastructure.Notifications.Repositories.NotificationRepository>();

        // Module 2 - Subscriptions
        services.AddScoped<PropertyOS.Application.Subscriptions.ISubscriptionService, PropertyOS.Infrastructure.Subscriptions.Services.SubscriptionService>();

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

                    // Module 4
                    npgsqlOptions.MapEnum<BuildingType>("building_type_enum");
                    npgsqlOptions.MapEnum<Governorate>("governorate_enum");
                    npgsqlOptions.MapEnum<FloorType>("floor_type_enum");
                    npgsqlOptions.MapEnum<OwnershipStatus>("ownership_status_enum");
                    npgsqlOptions.MapEnum<OccupancyStatus>("occupancy_status_enum");
                    npgsqlOptions.MapEnum<ParkingType>("parking_type_enum");
                    npgsqlOptions.MapEnum<ParkingAssignmentStatus>("parking_assignment_status_enum");

                    // Module 5
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractStatus>("contract_status_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Leasing.Enums.PaymentFrequency>("payment_frequency_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Leasing.Enums.TerminationType>("termination_type_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractDocumentType>("contract_document_type_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Leasing.Enums.LegalRegime>("legal_regime_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Leasing.Enums.TenantType>("tenant_type_enum");

                    // Module 6
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentPurpose>("payment_purpose_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentMethod>("payment_method_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Financials.Enums.DueDateStatus>("due_date_status_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Financials.Enums.ChequeStatus>("cheque_status_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Financials.Enums.AllocationStatus>("allocation_status_enum");

                    // Module 7
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Financials.Enums.ExpenseCategory>("expense_category_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Financials.Enums.ExpensePaymentMethod>("expense_payment_method_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Financials.Enums.ReceiptResetPolicy>("receipt_reset_policy_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Financials.Enums.EfawateercomStatus>("efawateercom_status_enum");

                    // Module 8
                    npgsqlOptions.MapEnum<MaintenanceCategory>("maintenance_category_enum");
                    npgsqlOptions.MapEnum<MaintenancePriority>("maintenance_priority_enum");
                    npgsqlOptions.MapEnum<MaintenanceStatus>("maintenance_status_enum");

                    // Module 9
                    npgsqlOptions.MapEnum<CurrencyCode>("currency_code_enum");
                    npgsqlOptions.MapEnum<ListingStatus>("listing_status_enum");
                    npgsqlOptions.MapEnum<ViewingRequestStatus>("viewing_request_status_enum");

                    // Module 11
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Notifications.Enums.NotificationType>("notification_type_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Notifications.Enums.NotificationStatus>("notification_status_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Notifications.Enums.NotificationPriority>("notification_priority_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Notifications.Enums.DeliveryChannel>("delivery_channel_enum");
                    npgsqlOptions.MapEnum<PropertyOS.Domain.Notifications.Enums.DeliveryStatus>("delivery_status_enum");
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
