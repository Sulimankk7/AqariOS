using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.UtilityBills;

namespace PropertyOS.Infrastructure.UtilityBills.Configurations;

internal sealed class UtilityBillConfiguration : IEntityTypeConfiguration<UtilityBill>
{
    public void Configure(EntityTypeBuilder<UtilityBill> builder)
    {
        builder.ToTable("utility_bills", t =>
        {
            t.HasCheckConstraint(
                "chk_utility_bills_amount_positive",
                "amount > 0");

            t.HasCheckConstraint(
                "chk_utility_bills_currency_length",
                "length(currency) = 3");

            t.HasCheckConstraint(
                "chk_utility_bills_notification_after_discovered",
                "notification_sent_at IS NULL OR notification_sent_at >= discovered_at");
        });

        // ── Unique Constraints ────────────────────────────────────────────────
        // DATABASE-LEVEL DUPLICATE PROTECTION (mandatory per spec §8)
        builder.HasIndex(b => new { b.UtilityAccountId, b.ProviderExternalId })
            .IsUnique()
            .HasDatabaseName("uq_utility_bills_account_external_id");


        builder.HasKey(b => b.Id);

        // ── Identity ──────────────────────────────────────────────────────────
        builder.Property(b => b.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(b => b.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(b => b.UtilityAccountId)
            .HasColumnName("utility_account_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(b => b.UtilityType)
            .HasColumnName("utility_type")
            .HasColumnType("utility_type_enum")
            .IsRequired();

        // ── Provider identity ─────────────────────────────────────────────────
        builder.Property(b => b.ProviderExternalId)
            .HasColumnName("provider_external_id")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired();

        // ── Bill content ──────────────────────────────────────────────────────
        builder.Property(b => b.BillDate)
            .HasColumnName("bill_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(b => b.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(b => b.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        builder.Property(b => b.Currency)
            .HasColumnName("currency")
            .HasColumnType("character(3)")
            .HasMaxLength(3)
            .HasDefaultValue("JOD")
            .IsRequired();

        builder.Property(b => b.IsPaid)
            .HasColumnName("is_paid")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(b => b.PaymentStatus)
            .HasColumnName("payment_status")
            .HasColumnType("utility_bill_status_enum")
            .HasDefaultValueSql("'unknown'")
            .IsRequired();

        builder.Property(b => b.ProviderReference)
            .HasColumnName("provider_reference")
            .HasColumnType("text")
            .IsRequired(false);

        // ── Discovery metadata ────────────────────────────────────────────────
        builder.Property(b => b.DiscoveredAt)
            .HasColumnName("discovered_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(b => b.IsFromHistoricalBackfill)
            .HasColumnName("is_from_historical_backfill")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        // ── Notification idempotency gate ─────────────────────────────────────
        builder.Property(b => b.NotificationSentAt)
            .HasColumnName("notification_sent_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        // ── Audit ─────────────────────────────────────────────────────────────
        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        // ── Concurrency token (PostgreSQL MVCC xmin) ──────────────────────────
        builder.Property(b => b.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        // ── Indexes ───────────────────────────────────────────────────────────

        // BILLS PER ACCOUNT newest-first (tenant bills page)
        builder.HasIndex(b => new { b.UtilityAccountId, b.BillDate })
            .HasDatabaseName("idx_utility_bills_account_date")
            .IsDescending(false, true); // BillDate DESC

        // COMPANY-LEVEL BILLING VIEW
        builder.HasIndex(b => new { b.CompanyId, b.BillDate })
            .HasDatabaseName("idx_utility_bills_company_date")
            .IsDescending(false, true); // BillDate DESC

        // NOTIFICATION DISPATCH GUARD — fast lookup for bills needing notification
        builder.HasIndex(b => b.UtilityAccountId)
            .HasDatabaseName("idx_utility_bills_notification_pending")
            .HasFilter("notification_sent_at IS NULL AND is_paid = false AND is_from_historical_backfill = false");
    }
}
