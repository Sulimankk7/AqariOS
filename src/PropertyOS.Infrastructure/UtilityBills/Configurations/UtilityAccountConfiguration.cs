using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Infrastructure.UtilityBills.Configurations;

internal sealed class UtilityAccountConfiguration : IEntityTypeConfiguration<UtilityAccount>
{
    public void Configure(EntityTypeBuilder<UtilityAccount> builder)
    {
        builder.ToTable("utility_accounts", t =>
        {
            // Check constraints
            t.HasCheckConstraint(
                "chk_utility_accounts_account_number_not_blank",
                "length(btrim(account_number)) > 0");

            t.HasCheckConstraint(
                "chk_utility_accounts_interval_consistent",
                "(billing_interval_sample_count = 0 AND average_billing_interval_days IS NULL " +
                " AND estimated_next_bill_date IS NULL) " +
                "OR (billing_interval_sample_count > 0 AND average_billing_interval_days IS NOT NULL)");

            t.HasCheckConstraint(
                "chk_utility_accounts_claim_fields_consistent",
                "(claimed_at IS NULL AND claimed_by_job_run_id IS NULL) " +
                "OR (claimed_at IS NOT NULL AND claimed_by_job_run_id IS NOT NULL)");

            t.HasCheckConstraint(
                "chk_utility_accounts_total_outstanding_nonneg",
                "total_outstanding_balance IS NULL OR total_outstanding_balance >= 0");
        });

        // ── Unique Constraints ────────────────────────────────────────────────
        // One electricity + one water account per lease
        builder.HasIndex(a => new { a.LeaseContractId, a.UtilityType })
            .IsUnique()
            .HasDatabaseName("uq_utility_accounts_lease_type")
            .HasFilter("deleted_at IS NULL");

        // Same physical account number cannot be linked twice for the same utility type
        builder.HasIndex(a => new { a.UtilityType, a.AccountNumber })
            .IsUnique()
            .HasDatabaseName("uq_utility_accounts_type_number")
            .HasFilter("deleted_at IS NULL");


        builder.HasKey(a => a.Id);

        // ── Identity ──────────────────────────────────────────────────────────
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.LeaseContractId)
            .HasColumnName("lease_contract_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.ApartmentId)
            .HasColumnName("apartment_id")
            .HasColumnType("uuid")
            .IsRequired();

        // ── Account details ───────────────────────────────────────────────────
        builder.Property(a => a.UtilityType)
            .HasColumnName("utility_type")
            .HasColumnType("utility_type_enum")
            .IsRequired();

        builder.Property(a => a.AccountNumber)
            .HasColumnName("account_number")
            .HasColumnType("character varying(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.MeterNumber)
            .HasColumnName("meter_number")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(a => a.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(a => a.TotalOutstandingBalance)
            .HasColumnName("total_outstanding_balance")
            .HasColumnType("numeric(12,3)")
            .IsRequired(false);

        // ── Sync state ────────────────────────────────────────────────────────
        builder.Property(a => a.LastKnownBillDate)
            .HasColumnName("last_known_bill_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(a => a.LastSuccessfulSyncAt)
            .HasColumnName("last_successful_sync_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(a => a.LastAttemptedSyncAt)
            .HasColumnName("last_attempted_sync_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(a => a.NextCheckAt)
            .HasColumnName("next_check_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(a => a.SyncStatus)
            .HasColumnName("sync_status")
            .HasColumnType("utility_sync_status_enum")
            .HasDefaultValueSql("'never_synced'")
            .IsRequired();

        builder.Property(a => a.LastSyncErrorDetail)
            .HasColumnName("last_sync_error_detail")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(a => a.ConsecutiveFailureCount)
            .HasColumnName("consecutive_failure_count")
            .HasColumnType("smallint")
            .HasDefaultValue((short)0)
            .IsRequired();

        // ── Water-only interval statistics ────────────────────────────────────
        builder.Property(a => a.AverageBillingIntervalDays)
            .HasColumnName("average_billing_interval_days")
            .HasColumnType("smallint")
            .IsRequired(false);

        builder.Property(a => a.BillingIntervalSampleCount)
            .HasColumnName("billing_interval_sample_count")
            .HasColumnType("smallint")
            .HasDefaultValue((short)0)
            .IsRequired();

        builder.Property(a => a.EstimatedNextBillDate)
            .HasColumnName("estimated_next_bill_date")
            .HasColumnType("date")
            .IsRequired(false);

        // ── Bootstrap flag ────────────────────────────────────────────────────
        builder.Property(a => a.HistoricalBootstrapCompleted)
            .HasColumnName("historical_bootstrap_completed")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        // ── Concurrency claim ─────────────────────────────────────────────────
        builder.Property(a => a.ClaimedByJobRunId)
            .HasColumnName("claimed_by_job_run_id")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(a => a.ClaimedAt)
            .HasColumnName("claimed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        // ── Soft delete ───────────────────────────────────────────────────────
        builder.Property(a => a.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(a => a.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        // ── Audit ─────────────────────────────────────────────────────────────
        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(a => a.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(a => a.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        // ── Concurrency token (PostgreSQL MVCC xmin) ──────────────────────────
        builder.Property(a => a.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        // ── Global query filter (soft-delete) ─────────────────────────────────
        builder.HasQueryFilter(a => a.DeletedAt == null);

        // ── Indexes ───────────────────────────────────────────────────────────

        // PRIMARY SCHEDULER INDEX — only bootstrapped, active, due accounts
        builder.HasIndex(a => new { a.UtilityType, a.NextCheckAt })
            .HasDatabaseName("idx_utility_accounts_scheduler")
            .HasFilter("deleted_at IS NULL AND is_active = true AND historical_bootstrap_completed = true");

        // BOOTSTRAP QUEUE — unbootstrapped accounts needing initial history import
        builder.HasIndex(a => new { a.UtilityType, a.CreatedAt })
            .HasDatabaseName("idx_utility_accounts_bootstrap_pending")
            .HasFilter("deleted_at IS NULL AND is_active = true AND historical_bootstrap_completed = false");

        // TENANT DASHBOARD — query handler uses explicit companyId filter (outside transaction)
        builder.HasIndex(a => new { a.CompanyId, a.TenantId })
            .HasDatabaseName("idx_utility_accounts_company_tenant")
            .HasFilter("deleted_at IS NULL");

        // LEASE LOOKUP
        builder.HasIndex(a => a.LeaseContractId)
            .HasDatabaseName("idx_utility_accounts_lease")
            .HasFilter("deleted_at IS NULL");

        // ── Relationships ─────────────────────────────────────────────────────
        // Intentionally no EF navigation properties to keep domain entities clean.
        // FK constraints are defined in the migration SQL directly.
    }
}
