using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Infrastructure.Financials.Configurations;

internal sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("payment_allocations", t =>
        {
            t.HasCheckConstraint("chk_payment_allocations_amount_positive", "allocated_amount > 0");
            t.HasCheckConstraint("chk_payment_allocations_not_self_referencing", "receiving_payment_id != obligation_payment_id");
            t.HasCheckConstraint("chk_payment_allocations_reversal_fields", "(allocation_status = 'active' AND reversal_reason IS NULL AND reversed_at IS NULL) OR (allocation_status = 'reversed' AND reversal_reason IS NOT NULL AND reversed_at IS NOT NULL)");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.ReceivingPaymentId)
            .HasColumnName("receiving_payment_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.ObligationPaymentId)
            .HasColumnName("obligation_payment_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.AllocatedAmount)
            .HasColumnName("allocated_amount")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        builder.Property(a => a.AllocationDate)
            .HasColumnName("allocation_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(a => a.AllocationStatus)
            .HasColumnName("allocation_status")
            .HasColumnType("allocation_status_enum")
            .HasDefaultValueSql("'active'")
            .IsRequired();

        builder.Property(a => a.ReversalReason)
            .HasColumnName("reversal_reason")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(a => a.ReversedAt)
            .HasColumnName("reversed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(a => a.ReversedBy)
            .HasColumnName("reversed_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(a => a.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired(false);

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

        builder.Property(a => a.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(a => a.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(a => a.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(a => a.CompanyId)
            .HasConstraintName("fk_payment_allocations_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RentPayment>()
            .WithMany()
            .HasForeignKey(a => new { a.CompanyId, a.ReceivingPaymentId })
            .HasPrincipalKey(p => new { p.CompanyId, p.Id })
            .HasConstraintName("fk_payment_allocations_rent_payments_receiving_payment")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RentPayment>()
            .WithMany()
            .HasForeignKey(a => new { a.CompanyId, a.ObligationPaymentId })
            .HasPrincipalKey(p => new { p.CompanyId, p.Id })
            .HasConstraintName("fk_payment_allocations_rent_payments_obligation_payment")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.ReversedBy)
            .HasConstraintName("fk_payment_allocations_users_reversed_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.CreatedBy)
            .HasConstraintName("fk_payment_allocations_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.UpdatedBy)
            .HasConstraintName("fk_payment_allocations_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.DeletedBy)
            .HasConstraintName("fk_payment_allocations_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.ObligationPaymentId)
            .HasDatabaseName("idx_payment_allocations_obligation_payment_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(a => a.ReceivingPaymentId)
            .HasDatabaseName("idx_payment_allocations_receiving_payment_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(a => new { a.CompanyId, a.AllocationDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_payment_allocations_company_allocation_date")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(a => new { a.CompanyId, a.AllocationStatus })
            .HasDatabaseName("idx_payment_allocations_status")
            .HasFilter("allocation_status = 'reversed' AND deleted_at IS NULL");

        builder.HasQueryFilter(a => a.DeletedAt == null);
    }
}
