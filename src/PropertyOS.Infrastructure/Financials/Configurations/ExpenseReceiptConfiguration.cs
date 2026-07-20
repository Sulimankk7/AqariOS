using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Infrastructure.Financials.Configurations;

internal sealed class ExpenseReceiptConfiguration : IEntityTypeConfiguration<ExpenseReceipt>
{
    public void Configure(EntityTypeBuilder<ExpenseReceipt> builder)
    {
        builder.ToTable("expense_receipts", t =>
        {
            t.HasCheckConstraint("chk_expense_receipts_amount_positive", "amount > 0");
            t.HasCheckConstraint("chk_expense_receipts_issued_at_not_future", "issued_at <= CURRENT_DATE");
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

        builder.Property(r => r.ExpenseId)
            .HasColumnName("expense_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(r => r.FileId)
            .HasColumnName("file_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(r => r.ReceiptNumber)
            .HasColumnName("receipt_number")
            .HasColumnType("character varying(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        builder.Property(r => r.IssuedAt)
            .HasColumnName("issued_at")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(r => r.UploadedBy)
            .HasColumnName("uploaded_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasColumnType("character varying(250)")
            .HasMaxLength(250)
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
            .HasName("uq_expense_receipts_company_id");

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(r => r.CompanyId)
            .HasConstraintName("fk_expense_receipts_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Expense)
            .WithMany(e => e.Receipts)
            .HasForeignKey(r => new { r.CompanyId, r.ExpenseId })
            .HasPrincipalKey(e => new { e.CompanyId, e.Id })
            .HasConstraintName("fk_expense_receipts_expenses_company_expense")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(r => r.UploadedBy)
            .HasConstraintName("fk_expense_receipts_users_uploaded_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(r => r.CreatedBy)
            .HasConstraintName("fk_expense_receipts_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(r => r.UpdatedBy)
            .HasConstraintName("fk_expense_receipts_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(r => r.DeletedBy)
            .HasConstraintName("fk_expense_receipts_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.CompanyId, r.ReceiptNumber })
            .HasDatabaseName("uq_expense_receipts_company_receipt_number")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(r => new { r.ExpenseId, r.FileId })
            .HasDatabaseName("uq_expense_receipts_expense_file")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(r => r.ExpenseId)
            .HasDatabaseName("idx_expense_receipts_expense_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(r => new { r.CompanyId, r.IssuedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_expense_receipts_company_issued_at")
            .HasFilter("deleted_at IS NULL");

        builder.HasQueryFilter(r => r.DeletedAt == null);
    }
}
