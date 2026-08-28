using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace PropertyOS.Tests.Integration.Infrastructure;

/// <summary>
/// Shared Npgsql data-source construction for integration tests that build their own DI
/// container against the Testcontainers database. Maps every native PostgreSQL enum the
/// platform uses (adding a new enum requires updating this list, DependencyInjection.cs,
/// PropertyOsDbContext.OnModelCreating, and PostgresTestFixture).
/// </summary>
public static class FinancialsTestDataSource
{
    public static NpgsqlDataSource Build(string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        MapAllEnums(dataSourceBuilder);
        return dataSourceBuilder.Build();
    }

    public static void ConfigureNpgsql(DbContextOptionsBuilder options, NpgsqlDataSource dataSource)
    {
        options.UseNpgsql(dataSource, o =>
        {
            o.MapEnum<PropertyOS.Domain.Companies.Enums.CompanyType>("company_type_enum");
            o.MapEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>("late_fee_type_enum");
            o.MapEnum<PropertyOS.Domain.Subscriptions.Enums.SubscriptionStatusEnum>("subscription_status_enum");
            o.MapEnum<PropertyOS.Domain.Subscriptions.Enums.BillingCycleEnum>("billing_cycle_enum");
            o.MapEnum<PropertyOS.Domain.Subscriptions.Enums.PlanChangeRequestStatus>("plan_change_request_status_enum");
            o.MapEnum<PropertyOS.Domain.Audit.Enums.AuditAction>("audit_action_enum");
            o.MapEnum<PropertyOS.Domain.Audit.Enums.AuditSeverity>("audit_severity_enum");
            o.MapEnum<PropertyOS.Domain.Audit.Enums.AuditSource>("audit_source_enum");
            o.MapEnum<PropertyOS.Domain.Identity.Enums.LoginStatus>("login_status_enum");
            o.MapEnum<PropertyOS.Domain.Identity.Enums.MembershipStatus>("membership_status_enum");
            o.MapEnum<PropertyOS.Domain.Identity.Enums.MfaType>("mfa_type_enum");
            o.MapEnum<PropertyOS.Domain.Identity.Enums.OtpPurpose>("otp_purpose_enum");
            o.MapEnum<PropertyOS.Domain.Identity.Enums.RevokeReason>("revoke_reason_enum");
            o.MapEnum<PropertyOS.Domain.Properties.Enums.BuildingType>("building_type_enum");
            o.MapEnum<PropertyOS.Domain.Properties.Enums.Governorate>("governorate_enum");
            o.MapEnum<PropertyOS.Domain.Properties.Enums.FloorType>("floor_type_enum");
            o.MapEnum<PropertyOS.Domain.Properties.Enums.OwnershipStatus>("ownership_status_enum");
            o.MapEnum<PropertyOS.Domain.Properties.Enums.OccupancyStatus>("occupancy_status_enum");
            o.MapEnum<PropertyOS.Domain.Properties.Enums.ParkingType>("parking_type_enum");
            o.MapEnum<PropertyOS.Domain.Properties.Enums.ParkingAssignmentStatus>("parking_assignment_status_enum");
            o.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractStatus>("contract_status_enum");
            o.MapEnum<PropertyOS.Domain.Leasing.Enums.PaymentFrequency>("payment_frequency_enum");
            o.MapEnum<PropertyOS.Domain.Leasing.Enums.TerminationType>("termination_type_enum");
            o.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractDocumentType>("contract_document_type_enum");
            o.MapEnum<PropertyOS.Domain.Leasing.Enums.LegalRegime>("legal_regime_enum");
            o.MapEnum<PropertyOS.Domain.Leasing.Enums.TenantType>("tenant_type_enum");
            o.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentPurpose>("payment_purpose_enum");
            o.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentMethod>("payment_method_enum");
            o.MapEnum<PropertyOS.Domain.Financials.Enums.DueDateStatus>("due_date_status_enum");
            o.MapEnum<PropertyOS.Domain.Financials.Enums.ChequeStatus>("cheque_status_enum");
            o.MapEnum<PropertyOS.Domain.Financials.Enums.AllocationStatus>("allocation_status_enum");
            o.MapEnum<PropertyOS.Domain.Financials.Enums.ExpenseCategory>("expense_category_enum");
            o.MapEnum<PropertyOS.Domain.Financials.Enums.ExpensePaymentMethod>("expense_payment_method_enum");
            o.MapEnum<PropertyOS.Domain.Financials.Enums.ReceiptResetPolicy>("receipt_reset_policy_enum");
            o.MapEnum<PropertyOS.Domain.Financials.Enums.EfawateercomStatus>("efawateercom_status_enum");
        });
    }

    private static void MapAllEnums(NpgsqlDataSourceBuilder dataSourceBuilder)
    {
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Companies.Enums.CompanyType>("company_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Companies.Enums.LateFeeType>("late_fee_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Subscriptions.Enums.SubscriptionStatusEnum>("subscription_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Subscriptions.Enums.BillingCycleEnum>("billing_cycle_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Subscriptions.Enums.PlanChangeRequestStatus>("plan_change_request_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Audit.Enums.AuditAction>("audit_action_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Audit.Enums.AuditSeverity>("audit_severity_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Audit.Enums.AuditSource>("audit_source_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.LoginStatus>("login_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.MembershipStatus>("membership_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.MfaType>("mfa_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.OtpPurpose>("otp_purpose_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Identity.Enums.RevokeReason>("revoke_reason_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.BuildingType>("building_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.Governorate>("governorate_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.FloorType>("floor_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.OwnershipStatus>("ownership_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.OccupancyStatus>("occupancy_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.ParkingType>("parking_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Properties.Enums.ParkingAssignmentStatus>("parking_assignment_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractStatus>("contract_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.PaymentFrequency>("payment_frequency_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.TerminationType>("termination_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.ContractDocumentType>("contract_document_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.LegalRegime>("legal_regime_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Leasing.Enums.TenantType>("tenant_type_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentPurpose>("payment_purpose_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.PaymentMethod>("payment_method_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.DueDateStatus>("due_date_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ChequeStatus>("cheque_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.AllocationStatus>("allocation_status_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ExpenseCategory>("expense_category_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ExpensePaymentMethod>("expense_payment_method_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.ReceiptResetPolicy>("receipt_reset_policy_enum");
        dataSourceBuilder.MapEnum<PropertyOS.Domain.Financials.Enums.EfawateercomStatus>("efawateercom_status_enum");
    }
}
