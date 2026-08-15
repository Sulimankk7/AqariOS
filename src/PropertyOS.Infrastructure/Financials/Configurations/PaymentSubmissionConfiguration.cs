using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Infrastructure.Financials.Configurations;

internal sealed class PaymentSubmissionConfiguration : IEntityTypeConfiguration<PaymentSubmission>
{
    public void Configure(EntityTypeBuilder<PaymentSubmission> builder)
    {
        builder.ToTable("payment_submissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedNever();

        builder.Property(p => p.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.RentPaymentId)
            .HasColumnName("rent_payment_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.PaymentMethod)
            .HasColumnName("payment_method")
            .HasColumnType("payment_method_enum")
            .IsRequired();

        builder.Property(p => p.ReferenceNumber)
            .HasColumnName("reference_number")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(p => p.ProofFileId)
            .HasColumnName("proof_file_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(p => p.ChequeNumber)
            .HasColumnName("cheque_number")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(p => p.BankName)
            .HasColumnName("bank_name")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(p => p.ChequeIssueDate)
            .HasColumnName("cheque_issue_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(p => p.ChequeDueDate)
            .HasColumnName("cheque_due_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasColumnType("submission_status_enum")
            .IsRequired();

        builder.Property(p => p.SubmittedBy)
            .HasColumnName("submitted_by")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.SubmittedAt)
            .HasColumnName("submitted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(p => p.VerifiedBy)
            .HasColumnName("verified_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(p => p.VerifiedAt)
            .HasColumnName("verified_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(p => p.RejectedBy)
            .HasColumnName("rejected_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(p => p.RejectedAt)
            .HasColumnName("rejected_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(p => p.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasColumnType("character varying(1000)")
            .HasMaxLength(1000)
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
            .IsRowVersion()
            .IsRequired();

        builder.HasQueryFilter(p => p.DeletedAt == null);

        builder.HasIndex(p => p.CompanyId)
            .HasDatabaseName("ix_payment_submissions_company_id");

        builder.HasIndex(p => p.RentPaymentId)
            .HasDatabaseName("ix_payment_submissions_rent_payment_id");
            
        builder.HasIndex(p => p.Status)
            .HasDatabaseName("ix_payment_submissions_status");
    }
}
