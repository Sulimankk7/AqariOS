using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Infrastructure.Leasing.Configurations;

internal sealed class ContractTerminationConfiguration : IEntityTypeConfiguration<ContractTermination>
{
    public void Configure(EntityTypeBuilder<ContractTermination> builder)
    {
        builder.ToTable("contract_terminations", t =>
        {
            t.HasCheckConstraint("chk_contract_terminations_balances_nonneg", "outstanding_balance >= 0 AND deposit_returned_amount >= 0 AND deposit_deduction_amount >= 0");
            t.HasCheckConstraint("chk_contract_terminations_deduction_reason", "deposit_deduction_amount = 0 OR deposit_deduction_reason IS NOT NULL");
            t.HasCheckConstraint("chk_contract_terminations_date_not_future", "termination_date <= CURRENT_DATE");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(t => t.LeaseContractId)
            .HasColumnName("lease_contract_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(t => t.TerminationType)
            .HasColumnName("termination_type")
            .HasColumnType("termination_type_enum")
            .IsRequired();

        builder.Property(t => t.TerminationDate)
            .HasColumnName("termination_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(t => t.Reason)
            .HasColumnName("reason")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(t => t.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(t => t.ApprovedBy)
            .HasColumnName("approved_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(t => t.OutstandingBalance)
            .HasColumnName("outstanding_balance")
            .HasColumnType("numeric(12,3)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(t => t.DepositReturnedAmount)
            .HasColumnName("deposit_returned_amount")
            .HasColumnType("numeric(12,3)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(t => t.DepositDeductionAmount)
            .HasColumnName("deposit_deduction_amount")
            .HasColumnType("numeric(12,3)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(t => t.DepositDeductionReason)
            .HasColumnName("deposit_deduction_reason")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(t => t.FinalUtilitySettlementCompleted)
            .HasColumnName("final_utility_settlement_completed")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(t => t.Currency)
            .HasColumnName("currency")
            .HasColumnType("character(3)")
            .HasMaxLength(3)
            .HasDefaultValueSql("'JOD'")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(t => t.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(t => t.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(t => t.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        // -----------------------------------------------------------------------
        // Foreign Keys
        // -----------------------------------------------------------------------
        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(t => t.CompanyId)
            .HasConstraintName("fk_contract_terminations_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LeaseContract>()
            .WithMany()
            .HasForeignKey(t => new { t.CompanyId, t.LeaseContractId })
            .HasPrincipalKey(c => new { c.CompanyId, c.Id })
            .HasConstraintName("fk_contract_terminations_lease_contracts_company_contract")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(t => t.ApprovedBy)
            .HasConstraintName("fk_contract_terminations_users_approved_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(t => t.CreatedBy)
            .HasConstraintName("fk_contract_terminations_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(t => t.UpdatedBy)
            .HasConstraintName("fk_contract_terminations_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(t => t.DeletedBy)
            .HasConstraintName("fk_contract_terminations_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        // -----------------------------------------------------------------------
        // Unique Constraints (Partial)
        // -----------------------------------------------------------------------
        builder.HasIndex(t => t.LeaseContractId)
            .HasDatabaseName("uq_contract_terminations_lease_contract_id")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        // Global query filter
        builder.HasQueryFilter(t => t.DeletedAt == null);
    }
}
