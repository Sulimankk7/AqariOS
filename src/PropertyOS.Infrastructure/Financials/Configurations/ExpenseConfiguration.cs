using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Infrastructure.Financials.Configurations;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses", t =>
        {
            t.HasCheckConstraint("chk_expenses_amount_positive", "amount > 0");
            t.HasCheckConstraint("chk_expenses_expense_date_not_future", "expense_date <= CURRENT_DATE");
            t.HasCheckConstraint("chk_expenses_building_overhead", "(building_id IS NOT NULL) OR (building_id IS NULL)"); // Just documenting building association options
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(e => e.BuildingId)
            .HasColumnName("building_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasColumnType("expense_category_enum")
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        builder.Property(e => e.Currency)
            .HasColumnName("currency")
            .HasColumnType("character(3)")
            .HasMaxLength(3)
            .HasDefaultValueSql("'JOD'")
            .IsRequired();

        builder.Property(e => e.ExpenseDate)
            .HasColumnName("expense_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(e => e.PaymentMethod)
            .HasColumnName("payment_method")
            .HasColumnType("expense_payment_method_enum")
            .IsRequired();

        builder.Property(e => e.VendorName)
            .HasColumnName("vendor_name")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(e => e.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasColumnType("character varying(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasColumnType("character varying(500)")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(e => e.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(e => e.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(e => e.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasAlternateKey(e => new { e.CompanyId, e.Id })
            .HasName("uq_expenses_company_id");

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .HasConstraintName("fk_expenses_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Building>()
            .WithMany()
            .HasForeignKey(e => e.BuildingId)
            .HasConstraintName("fk_expenses_buildings_building_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .HasConstraintName("fk_expenses_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(e => e.UpdatedBy)
            .HasConstraintName("fk_expenses_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(e => e.DeletedBy)
            .HasConstraintName("fk_expenses_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.CompanyId, e.ExpenseDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_expenses_company_date")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(e => new { e.CompanyId, e.Category, e.ExpenseDate })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_expenses_company_category_date")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(e => new { e.BuildingId, e.ExpenseDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_expenses_building_date")
            .HasFilter("building_id IS NOT NULL AND deleted_at IS NULL");

        builder.HasIndex(e => e.VendorName)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("idx_expenses_vendor_trgm");

        builder.HasIndex(e => e.InvoiceNumber)
            .HasDatabaseName("idx_expenses_invoice_number")
            .HasFilter("invoice_number IS NOT NULL AND deleted_at IS NULL");

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
