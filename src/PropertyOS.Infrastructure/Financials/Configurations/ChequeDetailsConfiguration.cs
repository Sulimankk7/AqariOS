using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Infrastructure.Financials.Configurations;

internal sealed class ChequeDetailsConfiguration : IEntityTypeConfiguration<ChequeDetails>
{
    public void Configure(EntityTypeBuilder<ChequeDetails> builder)
    {
        builder.ToTable("cheque_details", t =>
        {
            t.HasCheckConstraint("chk_cheque_details_amount_positive", "amount > 0");
            t.HasCheckConstraint("chk_cheque_details_due_after_issue", "due_date >= issue_date");
            t.HasCheckConstraint("chk_cheque_details_received_after_issue", "received_date IS NULL OR received_date >= issue_date");
            t.HasCheckConstraint("chk_cheque_details_deposit_requires_received", "deposit_date IS NULL OR received_date IS NOT NULL");
            t.HasCheckConstraint("chk_cheque_details_deposit_after_received", "deposit_date IS NULL OR deposit_date >= received_date");
            t.HasCheckConstraint("chk_cheque_details_clearance_requires_deposit", "clearance_date IS NULL OR deposit_date IS NOT NULL");
            t.HasCheckConstraint("chk_cheque_details_clearance_after_deposit", "clearance_date IS NULL OR clearance_date >= deposit_date");
            t.HasCheckConstraint("chk_cheque_details_bounce_requires_deposit", "bounce_date IS NULL OR deposit_date IS NOT NULL");
            t.HasCheckConstraint("chk_cheque_details_status_date_consistency", "(status = 'issued') OR (status = 'received' AND received_date IS NOT NULL) OR (status = 'deposited' AND deposit_date IS NOT NULL) OR (status = 'cleared' AND clearance_date IS NOT NULL) OR (status = 'bounced' AND bounce_date IS NOT NULL AND bounce_reason IS NOT NULL) OR (status = 'cancelled' AND cancellation_reason IS NOT NULL)");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(c => c.RentPaymentId)
            .HasColumnName("rent_payment_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(c => c.LeaseContractId)
            .HasColumnName("lease_contract_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(c => c.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(c => c.ChequeNumber)
            .HasColumnName("cheque_number")
            .HasColumnType("character varying(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.BankName)
            .HasColumnName("bank_name")
            .HasColumnType("character varying(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.BankBranch)
            .HasColumnName("bank_branch")
            .HasColumnType("character varying(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(c => c.IssueDate)
            .HasColumnName("issue_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(c => c.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(c => c.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        builder.Property(c => c.Currency)
            .HasColumnName("currency")
            .HasColumnType("character(3)")
            .HasMaxLength(3)
            .HasDefaultValueSql("'JOD'")
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasColumnType("cheque_status_enum")
            .HasDefaultValueSql("'issued'")
            .IsRequired();

        builder.Property(c => c.ReceivedDate)
            .HasColumnName("received_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(c => c.DepositDate)
            .HasColumnName("deposit_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(c => c.ClearanceDate)
            .HasColumnName("clearance_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(c => c.BounceDate)
            .HasColumnName("bounce_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(c => c.BounceReason)
            .HasColumnName("bounce_reason")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(c => c.BounceFeeCharged)
            .HasColumnName("bounce_fee_charged")
            .HasColumnType("numeric(12,3)")
            .IsRequired(false);

        builder.Property(c => c.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(c => c.ReplacementChequeId)
            .HasColumnName("replacement_cheque_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(c => c.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(c => c.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(c => c.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(c => c.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(c => c.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasAlternateKey(c => new { c.CompanyId, c.Id })
            .HasName("uq_cheque_details_company_id");

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(c => c.CompanyId)
            .HasConstraintName("fk_cheque_details_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RentPayment>()
            .WithOne()
            .HasForeignKey<ChequeDetails>(c => new { c.CompanyId, c.RentPaymentId })
            .HasPrincipalKey<RentPayment>(p => new { p.CompanyId, p.Id })
            .HasConstraintName("fk_cheque_details_rent_payments_company_payment")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LeaseContract>()
            .WithMany()
            .HasForeignKey(c => new { c.CompanyId, c.LeaseContractId })
            .HasPrincipalKey(l => new { l.CompanyId, l.Id })
            .HasConstraintName("fk_cheque_details_lease_contracts_company_contract")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(c => new { c.CompanyId, c.TenantId })
            .HasPrincipalKey(t => new { t.CompanyId, t.Id })
            .HasConstraintName("fk_cheque_details_tenants_company_tenant")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ChequeDetails>()
            .WithMany()
            .HasForeignKey(c => new { c.CompanyId, c.ReplacementChequeId })
            .HasPrincipalKey(c => new { c.CompanyId, c.Id })
            .HasConstraintName("fk_cheque_details_self_replacement_cheque")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(c => c.CreatedBy)
            .HasConstraintName("fk_cheque_details_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(c => c.UpdatedBy)
            .HasConstraintName("fk_cheque_details_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(c => c.DeletedBy)
            .HasConstraintName("fk_cheque_details_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.RentPaymentId)
            .HasDatabaseName("uq_cheque_details_rent_payment_id")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(c => new { c.CompanyId, c.ChequeNumber, c.BankName })
            .HasDatabaseName("uq_cheque_details_company_cheque_number")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(c => new { c.LeaseContractId, c.DueDate })
            .HasDatabaseName("idx_cheque_details_lease_contract_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(c => new { c.CompanyId, c.Status, c.DueDate })
            .HasDatabaseName("idx_cheque_details_company_status_due_date")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(c => c.TenantId)
            .HasDatabaseName("idx_cheque_details_tenant_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
