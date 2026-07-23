using Microsoft.EntityFrameworkCore;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Properties;

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
    // Module 4 — Properties (Phase 1: buildings, building_addresses, floors, apartments)
    // ---------------------------------------------------------------------------

    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<BuildingAddress> BuildingAddresses => Set<BuildingAddress>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Apartment> Apartments => Set<Apartment>();
    public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
    public DbSet<ParkingAssignment> ParkingAssignments => Set<ParkingAssignment>();

    // ---------------------------------------------------------------------------
    // Module 5 — Leasing (Phase 1: tenants, tenant_family_members, tenant_emergency_contacts, tenant_vehicles)
    // ---------------------------------------------------------------------------

    public DbSet<PropertyOS.Domain.Leasing.Tenant> Tenants => Set<PropertyOS.Domain.Leasing.Tenant>();
    public DbSet<PropertyOS.Domain.Leasing.TenantFamilyMember> TenantFamilyMembers => Set<PropertyOS.Domain.Leasing.TenantFamilyMember>();
    public DbSet<PropertyOS.Domain.Leasing.TenantEmergencyContact> TenantEmergencyContacts => Set<PropertyOS.Domain.Leasing.TenantEmergencyContact>();
    public DbSet<PropertyOS.Domain.Leasing.TenantVehicle> TenantVehicles => Set<PropertyOS.Domain.Leasing.TenantVehicle>();

    public DbSet<PropertyOS.Domain.Leasing.LeaseContract> LeaseContracts => Set<PropertyOS.Domain.Leasing.LeaseContract>();
    public DbSet<PropertyOS.Domain.Leasing.ContractTermination> ContractTerminations => Set<PropertyOS.Domain.Leasing.ContractTermination>();
    public DbSet<PropertyOS.Domain.Leasing.ContractStatusHistory> ContractStatusHistory => Set<PropertyOS.Domain.Leasing.ContractStatusHistory>();
    public DbSet<PropertyOS.Domain.Leasing.ContractDocument> ContractDocuments => Set<PropertyOS.Domain.Leasing.ContractDocument>();

    // ---------------------------------------------------------------------------
    // Module 6 — Rent Payments & Cheques
    // ---------------------------------------------------------------------------

    public DbSet<PropertyOS.Domain.Financials.RentPayment> RentPayments => Set<PropertyOS.Domain.Financials.RentPayment>();
    public DbSet<PropertyOS.Domain.Financials.ChequeDetails> ChequeDetails => Set<PropertyOS.Domain.Financials.ChequeDetails>();
    public DbSet<PropertyOS.Domain.Financials.PaymentAllocation> PaymentAllocations => Set<PropertyOS.Domain.Financials.PaymentAllocation>();

    // ---------------------------------------------------------------------------
    // Module 7 — Financial Operations
    // ---------------------------------------------------------------------------

    public DbSet<PropertyOS.Domain.Financials.Expense> Expenses => Set<PropertyOS.Domain.Financials.Expense>();
    public DbSet<PropertyOS.Domain.Financials.ExpenseReceipt> ExpenseReceipts => Set<PropertyOS.Domain.Financials.ExpenseReceipt>();
    public DbSet<PropertyOS.Domain.Financials.RentPaymentReceipt> RentPaymentReceipts => Set<PropertyOS.Domain.Financials.RentPaymentReceipt>();
    public DbSet<PropertyOS.Domain.Financials.CompanyReceiptSequence> CompanyReceiptSequences => Set<PropertyOS.Domain.Financials.CompanyReceiptSequence>();
    public DbSet<PropertyOS.Domain.Financials.EfawateercomTransaction> EfawateercomTransactions => Set<PropertyOS.Domain.Financials.EfawateercomTransaction>();

    // ---------------------------------------------------------------------------
    // Module 8 — Maintenance
    // ---------------------------------------------------------------------------

    public DbSet<PropertyOS.Domain.Maintenance.MaintenanceRequest> MaintenanceRequests => Set<PropertyOS.Domain.Maintenance.MaintenanceRequest>();
    public DbSet<PropertyOS.Domain.Maintenance.MaintenanceRequestAttachment> MaintenanceRequestAttachments => Set<PropertyOS.Domain.Maintenance.MaintenanceRequestAttachment>();
    public DbSet<PropertyOS.Domain.Maintenance.MaintenanceRequestComment> MaintenanceRequestComments => Set<PropertyOS.Domain.Maintenance.MaintenanceRequestComment>();
    public DbSet<PropertyOS.Domain.Maintenance.MaintenanceStatusHistory> MaintenanceStatusHistory => Set<PropertyOS.Domain.Maintenance.MaintenanceStatusHistory>();

    // ---------------------------------------------------------------------------
    // Module 9 — Marketplace
    // ---------------------------------------------------------------------------
    public DbSet<PropertyOS.Domain.Marketplace.MarketplaceListing> MarketplaceListings => Set<PropertyOS.Domain.Marketplace.MarketplaceListing>();
    public DbSet<PropertyOS.Domain.Marketplace.ListingImage> ListingImages => Set<PropertyOS.Domain.Marketplace.ListingImage>();
    public DbSet<PropertyOS.Domain.Marketplace.ViewingRequest> ViewingRequests => Set<PropertyOS.Domain.Marketplace.ViewingRequest>();

    // ---------------------------------------------------------------------------
    // Module 10 — Documents & Central File Storage
    // ---------------------------------------------------------------------------
    public DbSet<PropertyOS.Domain.Files.Entities.FileStorage> FileStorage => Set<PropertyOS.Domain.Files.Entities.FileStorage>();
    public DbSet<PropertyOS.Domain.Documents.Entities.DocumentCategory> DocumentCategories => Set<PropertyOS.Domain.Documents.Entities.DocumentCategory>();
    public DbSet<PropertyOS.Domain.Documents.Entities.BuildingDocument> BuildingDocuments => Set<PropertyOS.Domain.Documents.Entities.BuildingDocument>();

    // ---------------------------------------------------------------------------
    // Module 11 — Notifications
    // ---------------------------------------------------------------------------
    public DbSet<PropertyOS.Domain.Notifications.NotificationTemplate> NotificationTemplates => Set<PropertyOS.Domain.Notifications.NotificationTemplate>();
    public DbSet<PropertyOS.Domain.Notifications.Notification> Notifications => Set<PropertyOS.Domain.Notifications.Notification>();
    public DbSet<PropertyOS.Domain.Notifications.NotificationDelivery> NotificationDeliveries => Set<PropertyOS.Domain.Notifications.NotificationDelivery>();

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
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Companies.Enums.CompanyType>(name: "company_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>(name: "late_fee_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Subscriptions.Enums.SubscriptionStatusEnum>(name: "subscription_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Subscriptions.Enums.BillingCycleEnum>(name: "billing_cycle_enum");

        // Module 3
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Identity.Enums.MfaType>(name: "mfa_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Identity.Enums.MembershipStatus>(name: "membership_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Identity.Enums.RevokeReason>(name: "revoke_reason_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Identity.Enums.LoginStatus>(name: "login_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Identity.Enums.OtpPurpose>(name: "otp_purpose_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Audit.Enums.AuditAction>(name: "audit_action_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Audit.Enums.AuditSeverity>(name: "audit_severity_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Audit.Enums.AuditSource>(name: "audit_source_enum");

        // Module 4
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Properties.Enums.BuildingType>(name: "building_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Properties.Enums.Governorate>(name: "governorate_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Properties.Enums.FloorType>(name: "floor_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Properties.Enums.OwnershipStatus>(name: "ownership_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Properties.Enums.OccupancyStatus>(name: "occupancy_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Properties.Enums.ParkingType>(name: "parking_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Properties.Enums.ParkingAssignmentStatus>(name: "parking_assignment_status_enum");

        // Module 5
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Leasing.Enums.ContractStatus>(name: "contract_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Leasing.Enums.PaymentFrequency>(name: "payment_frequency_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Leasing.Enums.TerminationType>(name: "termination_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Leasing.Enums.ContractDocumentType>(name: "contract_document_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Leasing.Enums.LegalRegime>(name: "legal_regime_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Leasing.Enums.TenantType>(name: "tenant_type_enum");

        // Module 6
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Financials.Enums.PaymentPurpose>(name: "payment_purpose_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Financials.Enums.PaymentMethod>(name: "payment_method_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Financials.Enums.DueDateStatus>(name: "due_date_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Financials.Enums.ChequeStatus>(name: "cheque_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Financials.Enums.AllocationStatus>(name: "allocation_status_enum");

        // Module 7
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Financials.Enums.ExpenseCategory>(name: "expense_category_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Financials.Enums.ExpensePaymentMethod>(name: "expense_payment_method_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Financials.Enums.ReceiptResetPolicy>(name: "receipt_reset_policy_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Financials.Enums.EfawateercomStatus>(name: "efawateercom_status_enum");

        // Module 8
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Maintenance.Enums.MaintenanceCategory>(name: "maintenance_category_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Maintenance.Enums.MaintenancePriority>(name: "maintenance_priority_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Maintenance.Enums.MaintenanceStatus>(name: "maintenance_status_enum");

        // Module 9
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Common.Enums.CurrencyCode>(name: "currency_code_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Marketplace.Enums.ListingStatus>(name: "listing_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Marketplace.Enums.ViewingRequestStatus>(name: "viewing_request_status_enum");


        // Module 11
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Notifications.Enums.NotificationType>(name: "notification_type_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Notifications.Enums.NotificationStatus>(name: "notification_status_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Notifications.Enums.NotificationPriority>(name: "notification_priority_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Notifications.Enums.DeliveryChannel>(name: "delivery_channel_enum");
        modelBuilder.HasPostgresEnum<PropertyOS.Domain.Notifications.Enums.DeliveryStatus>(name: "delivery_status_enum");

        // All entity configurations are discovered from IEntityTypeConfiguration<T>
        // classes in this assembly. This is the only call in OnModelCreating —
        // per the approved architecture, no configuration logic lives here.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropertyOsDbContext).Assembly);
    }
}
