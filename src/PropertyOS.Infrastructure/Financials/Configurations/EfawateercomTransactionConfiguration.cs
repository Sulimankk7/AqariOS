using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Infrastructure.Financials.Configurations;

internal sealed class EfawateercomTransactionConfiguration : IEntityTypeConfiguration<EfawateercomTransaction>
{
    public void Configure(EntityTypeBuilder<EfawateercomTransaction> builder)
    {
        builder.ToTable("efawateercom_transactions", t =>
        {
            t.HasCheckConstraint("chk_efawateercom_transactions_amount_positive", "amount > 0");
            t.HasCheckConstraint("chk_efawateercom_transactions_response_time_after_request", "response_time IS NULL OR response_time >= request_time");
            t.HasCheckConstraint("chk_efawateercom_transactions_status_consistency", "(transaction_status IN ('pending', 'sent')) OR (transaction_status IN ('success', 'failed', 'timeout', 'cancelled') AND response_time IS NOT NULL)");
        });

        builder.HasKey(tx => tx.Id);

        builder.Property(tx => tx.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(tx => tx.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(tx => tx.RentPaymentId)
            .HasColumnName("rent_payment_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(tx => tx.ExternalTransactionId)
            .HasColumnName("external_transaction_id")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(tx => tx.PaymentReference)
            .HasColumnName("payment_reference")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(tx => tx.RequestTime)
            .HasColumnName("request_time")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(tx => tx.ResponseTime)
            .HasColumnName("response_time")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(tx => tx.TransactionStatus)
            .HasColumnName("transaction_status")
            .HasColumnType("efawateercom_status_enum")
            .HasDefaultValueSql("'pending'")
            .IsRequired();

        builder.Property(tx => tx.ResponseCode)
            .HasColumnName("response_code")
            .HasColumnType("character varying(20)")
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(tx => tx.ResponseMessage)
            .HasColumnName("response_message")
            .HasColumnType("character varying(250)")
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(tx => tx.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        builder.Property(tx => tx.Currency)
            .HasColumnName("currency")
            .HasColumnType("character(3)")
            .HasMaxLength(3)
            .HasDefaultValueSql("'JOD'")
            .IsRequired();

        builder.Property(tx => tx.RawResponse)
            .HasColumnName("raw_response")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(tx => tx.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(tx => tx.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(tx => tx.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(tx => tx.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(tx => tx.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(tx => tx.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(tx => tx.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasAlternateKey(tx => new { tx.CompanyId, tx.Id })
            .HasName("uq_efawateercom_transactions_company_id");

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(tx => tx.CompanyId)
            .HasConstraintName("fk_efawateercom_transactions_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tx => tx.RentPayment)
            .WithMany()
            .HasForeignKey(tx => new { tx.CompanyId, tx.RentPaymentId })
            .HasPrincipalKey(p => new { p.CompanyId, p.Id })
            .HasConstraintName("fk_efawateercom_transactions_rent_payments_company_payment")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(tx => tx.CreatedBy)
            .HasConstraintName("fk_efawateercom_transactions_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(tx => tx.UpdatedBy)
            .HasConstraintName("fk_efawateercom_transactions_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(tx => tx.DeletedBy)
            .HasConstraintName("fk_efawateercom_transactions_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(tx => tx.ExternalTransactionId)
            .HasDatabaseName("uq_efawateercom_transactions_external_transaction_id")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(tx => tx.RentPaymentId)
            .HasDatabaseName("idx_efawateercom_transactions_rent_payment_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(tx => new { tx.CompanyId, tx.TransactionStatus, tx.RequestTime })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_efawateercom_transactions_company_status_request_time")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(tx => tx.PaymentReference)
            .HasDatabaseName("idx_efawateercom_transactions_payment_reference")
            .HasFilter("payment_reference IS NOT NULL AND deleted_at IS NULL");

        builder.HasQueryFilter(tx => tx.DeletedAt == null);
    }
}
