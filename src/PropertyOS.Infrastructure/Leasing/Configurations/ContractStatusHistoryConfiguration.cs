using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Infrastructure.Leasing.Configurations;

internal sealed class ContractStatusHistoryConfiguration : IEntityTypeConfiguration<ContractStatusHistory>
{
    public void Configure(EntityTypeBuilder<ContractStatusHistory> builder)
    {
        builder.ToTable("contract_status_history", t =>
        {
            t.HasCheckConstraint("chk_contract_status_history_no_noop", "previous_status IS NULL OR previous_status != new_status");
        });

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(h => h.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(h => h.LeaseContractId)
            .HasColumnName("lease_contract_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(h => h.PreviousStatus)
            .HasColumnName("previous_status")
            .HasColumnType("contract_status_enum")
            .IsRequired(false);

        builder.Property(h => h.NewStatus)
            .HasColumnName("new_status")
            .HasColumnType("contract_status_enum")
            .IsRequired();

        builder.Property(h => h.ChangedBy)
            .HasColumnName("changed_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(h => h.ChangedAt)
            .HasColumnName("changed_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(h => h.Reason)
            .HasColumnName("reason")
            .HasColumnType("text")
            .IsRequired(false);

        // -----------------------------------------------------------------------
        // Foreign Keys
        // -----------------------------------------------------------------------
        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(h => h.CompanyId)
            .HasConstraintName("fk_contract_status_history_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LeaseContract>()
            .WithMany()
            .HasForeignKey(h => new { h.CompanyId, h.LeaseContractId })
            .HasPrincipalKey(c => new { c.CompanyId, c.Id })
            .HasConstraintName("fk_contract_status_history_lease_contracts_company_contract")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(h => h.ChangedBy)
            .HasConstraintName("fk_contract_status_history_users_changed_by")
            .OnDelete(DeleteBehavior.SetNull);

        // -----------------------------------------------------------------------
        // Indexes
        // -----------------------------------------------------------------------
        builder.HasIndex(h => new { h.LeaseContractId, h.ChangedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_contract_status_history_contract_changed_at");

        builder.HasIndex(h => new { h.CompanyId, h.NewStatus, h.ChangedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_contract_status_history_company_new_status");
    }
}
