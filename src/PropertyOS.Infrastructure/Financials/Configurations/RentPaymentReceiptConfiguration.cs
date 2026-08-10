using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Infrastructure.Financials.Configurations;

internal sealed class RentPaymentReceiptConfiguration : IEntityTypeConfiguration<RentPaymentReceipt>
{
    public void Configure(EntityTypeBuilder<RentPaymentReceipt> builder)
    {
        builder.ToTable("rent_payment_receipts", t =>
        {
            t.HasCheckConstraint("chk_rent_payment_receipts_amount_positive", "amount > 0");
            t.HasCheckConstraint("chk_rent_payment_receipts_issue_date_not_future", "issue_date <= CURRENT_DATE");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(r => r.RentPaymentId)
            .HasColumnName("rent_payment_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(r => r.ReceiptNumber)
            .HasColumnName("receipt_number")
            .HasColumnType("character varying(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.IssueDate)
            .HasColumnName("issue_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(r => r.IssuedBy)
            .HasColumnName("issued_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(r => r.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        builder.Property(r => r.Currency)
            .HasColumnName("currency")
            .HasColumnType("character(3)")
            .HasMaxLength(3)
            .HasDefaultValueSql("'JOD'")
            .IsRequired();

        builder.Property(r => r.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(r => r.FileId)
            .HasColumnName("file_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(r => r.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(r => r.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(r => r.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(r => r.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(r => r.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasAlternateKey(r => new { r.CompanyId, r.Id })
            .HasName("uq_rent_payment_receipts_company_id");

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(r => r.CompanyId)
            .HasConstraintName("fk_rent_payment_receipts_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RentPayment)
            .WithOne(p => p.Receipt)
            .HasForeignKey<RentPaymentReceipt>(r => new { r.CompanyId, r.RentPaymentId })
            .HasPrincipalKey<RentPayment>(p => new { p.CompanyId, p.Id })
            .HasConstraintName("fk_rent_payment_receipts_rent_payments_company_payment")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(r => r.IssuedBy)
            .HasConstraintName("fk_rent_payment_receipts_users_issued_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(r => r.CreatedBy)
            .HasConstraintName("fk_rent_payment_receipts_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(r => r.UpdatedBy)
            .HasConstraintName("fk_rent_payment_receipts_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(r => r.DeletedBy)
            .HasConstraintName("fk_rent_payment_receipts_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.RentPaymentId)
            .HasDatabaseName("uq_rent_payment_receipts_rent_payment_id")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(r => new { r.CompanyId, r.ReceiptNumber })
            .HasDatabaseName("uq_rent_payment_receipts_company_receipt_number")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(r => new { r.CompanyId, r.IssueDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_rent_payment_receipts_company_issue_date")
            .HasFilter("deleted_at IS NULL");

        builder.HasQueryFilter(r => r.DeletedAt == null);
    }
}
