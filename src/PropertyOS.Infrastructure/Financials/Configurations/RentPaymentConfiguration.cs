using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Infrastructure.Financials.Configurations;

internal sealed class RentPaymentConfiguration : IEntityTypeConfiguration<RentPayment>
{
    public void Configure(EntityTypeBuilder<RentPayment> builder)
    {
        builder.ToTable("rent_payments", t =>
        {
            t.HasCheckConstraint("chk_rent_payments_period_dates", "billing_period_end IS NULL OR billing_period_start IS NULL OR billing_period_end > billing_period_start");
            t.HasCheckConstraint("chk_rent_payments_amount_due_nonneg", "amount_due >= 0");
            t.HasCheckConstraint("chk_rent_payments_amount_paid_nonneg", "amount_paid >= 0");
            t.HasCheckConstraint("chk_rent_payments_scheduled_period_purpose", "(payment_purpose = 'scheduled_installment' AND billing_period_start IS NOT NULL AND billing_period_end IS NOT NULL AND due_date IS NOT NULL) OR (payment_purpose IN ('unallocated_receipt', 'adjustment') AND billing_period_start IS NULL AND billing_period_end IS NULL)");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.LeaseContractId)
            .HasColumnName("lease_contract_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.BuildingId)
            .HasColumnName("building_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.ApartmentId)
            .HasColumnName("apartment_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.BillingPeriodStart)
            .HasColumnName("billing_period_start")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(p => p.BillingPeriodEnd)
            .HasColumnName("billing_period_end")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(p => p.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(p => p.AmountDue)
            .HasColumnName("amount_due")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        // App-maintained settlement cache: written transactionally by the allocation
        // handlers under the FOR UPDATE protocol. Deliberately NOT ValueGeneratedOnAddOrUpdate —
        // that would make EF silently drop every UPDATE to this column (there is no DB
        // maintenance trigger; integrity is backstopped by trg_payment_allocations_enforce_limits).
        builder.Property(p => p.AmountPaid)
            .HasColumnName("amount_paid")
            .HasColumnType("numeric(12,3)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasColumnName("currency")
            .HasColumnType("character(3)")
            .HasMaxLength(3)
            .HasDefaultValueSql("'JOD'")
            .IsRequired();

        builder.Property(p => p.PaymentPurpose)
            .HasColumnName("payment_purpose")
            .HasColumnType("payment_purpose_enum")
            .HasDefaultValueSql("'scheduled_installment'")
            .IsRequired();

        builder.Property(p => p.PaymentMethod)
            .HasColumnName("payment_method")
            .HasColumnType("payment_method_enum")
            .IsRequired(false);

        builder.Property(p => p.PaymentReferenceNumber)
            .HasColumnName("payment_reference_number")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(p => p.ReceiptNumber)
            .HasColumnName("receipt_number")
            .HasColumnType("character varying(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        // App-maintained (see AmountPaid note above); also written by RentPayment.Cancel.
        builder.Property(p => p.DueDateStatus)
            .HasColumnName("due_date_status")
            .HasColumnType("due_date_status_enum")
            .HasDefaultValueSql("'pending'")
            .IsRequired();

        builder.Property(p => p.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(p => p.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(p => p.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(p => p.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasAlternateKey(p => new { p.CompanyId, p.Id })
            .HasName("uq_rent_payments_company_id");

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(p => p.CompanyId)
            .HasConstraintName("fk_rent_payments_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LeaseContract>()
            .WithMany()
            .HasForeignKey(p => new { p.CompanyId, p.LeaseContractId })
            .HasPrincipalKey(c => new { c.CompanyId, c.Id })
            .HasConstraintName("fk_rent_payments_lease_contracts_company_contract")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(p => new { p.CompanyId, p.TenantId })
            .HasPrincipalKey(t => new { t.CompanyId, t.Id })
            .HasConstraintName("fk_rent_payments_tenants_company_tenant")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Building>()
            .WithMany()
            .HasForeignKey(p => p.BuildingId)
            .HasConstraintName("fk_rent_payments_buildings_building_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(p => new { p.CompanyId, p.ApartmentId })
            .HasPrincipalKey(a => new { a.CompanyId, a.Id })
            .HasConstraintName("fk_rent_payments_apartments_company_apartment")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .HasConstraintName("fk_rent_payments_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(p => p.UpdatedBy)
            .HasConstraintName("fk_rent_payments_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(p => p.DeletedBy)
            .HasConstraintName("fk_rent_payments_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => new { p.LeaseContractId, p.DueDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_rent_payments_contract_due_date")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(p => new { p.TenantId, p.DueDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_rent_payments_tenant_due_date")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(p => new { p.CompanyId, p.DueDateStatus, p.DueDate })
            .HasDatabaseName("idx_rent_payments_company_status_due_date")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(p => new { p.CompanyId, p.DueDate })
            .HasDatabaseName("idx_rent_payments_late_overdue")
            .HasFilter("due_date_status IN ('late', 'overdue_unpaid') AND deleted_at IS NULL");

        builder.HasIndex(p => new { p.ApartmentId, p.DueDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_rent_payments_apartment_due_date")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(p => p.ReceiptNumber)
            .HasDatabaseName("idx_rent_payments_receipt_number")
            .HasFilter("receipt_number IS NOT NULL AND deleted_at IS NULL");

        builder.HasIndex(p => new { p.LeaseContractId, p.BillingPeriodStart, p.BillingPeriodEnd })
            .HasDatabaseName("uq_rent_payments_contract_period")
            .IsUnique()
            .HasFilter("deleted_at IS NULL AND payment_purpose = 'scheduled_installment'");

        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
