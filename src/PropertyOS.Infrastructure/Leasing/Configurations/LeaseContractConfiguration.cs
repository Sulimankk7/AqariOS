using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Infrastructure.Leasing.Configurations;

internal sealed class LeaseContractConfiguration : IEntityTypeConfiguration<LeaseContract>
{
    public void Configure(EntityTypeBuilder<LeaseContract> builder)
    {
        builder.ToTable("lease_contracts", t =>
        {
            t.HasCheckConstraint("chk_lease_contracts_dates", "end_date > start_date");
            t.HasCheckConstraint("chk_lease_contracts_signed_date", "signed_date IS NULL OR signed_date <= end_date");
            t.HasCheckConstraint("chk_lease_contracts_monthly_rent_positive", "monthly_rent_amount > 0");
            t.HasCheckConstraint("chk_lease_contracts_deposit_nonneg", "security_deposit_amount >= 0");
            t.HasCheckConstraint("chk_lease_contracts_payment_due_day", "payment_due_day BETWEEN 1 AND 28");
            t.HasCheckConstraint("chk_lease_contracts_no_self_reference", "prior_contract_id IS NULL OR prior_contract_id != id");
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

        builder.Property(c => c.BuildingId)
            .HasColumnName("building_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(c => c.ApartmentId)
            .HasColumnName("apartment_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(c => c.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(c => c.PriorContractId)
            .HasColumnName("prior_contract_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(c => c.ContractNumber)
            .HasColumnName("contract_number")
            .HasColumnType("character varying(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.LegalRegime)
            .HasColumnName("legal_regime")
            .HasColumnType("legal_regime_enum")
            .HasDefaultValueSql("'standard'")
            .IsRequired();

        builder.Property(c => c.TenantType)
            .HasColumnName("tenant_type")
            .HasColumnType("tenant_type_enum")
            .HasDefaultValueSql("'personal'")
            .IsRequired();

        builder.Property(c => c.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(c => c.EndDate)
            .HasColumnName("end_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(c => c.SignedDate)
            .HasColumnName("signed_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(c => c.MonthlyRentAmount)
            .HasColumnName("monthly_rent_amount")
            .HasColumnType("numeric(12,3)")
            .IsRequired();

        builder.Property(c => c.Currency)
            .HasColumnName("currency")
            .HasColumnType("character(3)")
            .HasMaxLength(3)
            .HasDefaultValueSql("'JOD'")
            .IsRequired();

        builder.Property(c => c.SecurityDepositAmount)
            .HasColumnName("security_deposit_amount")
            .HasColumnType("numeric(12,3)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(c => c.PaymentFrequency)
            .HasColumnName("payment_frequency")
            .HasColumnType("payment_frequency_enum")
            .HasDefaultValueSql("'monthly'")
            .IsRequired();

        builder.Property(c => c.PaymentDueDay)
            .HasColumnName("payment_due_day")
            .HasColumnType("smallint")
            .HasDefaultValue((short)1)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasColumnType("contract_status_enum")
            .HasDefaultValueSql("'draft'")
            .IsRequired();

        builder.Property(c => c.ExternalRegistrationRef)
            .HasColumnName("external_registration_ref")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(c => c.ContractDocumentId)
            .HasColumnName("contract_document_id")
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

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        // -----------------------------------------------------------------------
        // Candidate key: UNIQUE(company_id, id)
        // -----------------------------------------------------------------------
        builder.HasAlternateKey(c => new { c.CompanyId, c.Id })
            .HasName("uq_lease_contracts_company_id");

        // -----------------------------------------------------------------------
        // Foreign Keys
        // -----------------------------------------------------------------------
        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(c => c.CompanyId)
            .HasConstraintName("fk_lease_contracts_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Properties.Building>()
            .WithMany()
            .HasForeignKey(c => c.BuildingId)
            .HasConstraintName("fk_lease_contracts_buildings_building_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Properties.Apartment>()
            .WithMany()
            .HasForeignKey(c => new { c.CompanyId, c.ApartmentId })
            .HasPrincipalKey(a => new { a.CompanyId, a.Id })
            .HasConstraintName("fk_lease_contracts_apartments_company_apartment")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(c => new { c.CompanyId, c.TenantId })
            .HasPrincipalKey(t => new { t.CompanyId, t.Id })
            .HasConstraintName("fk_lease_contracts_tenants_company_tenant")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LeaseContract>()
            .WithMany()
            .HasForeignKey(c => new { c.CompanyId, c.PriorContractId })
            .HasPrincipalKey(c => new { c.CompanyId, c.Id })
            .HasConstraintName("fk_lease_contracts_self_prior_contract")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(c => c.CreatedBy)
            .HasConstraintName("fk_lease_contracts_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(c => c.UpdatedBy)
            .HasConstraintName("fk_lease_contracts_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(c => c.DeletedBy)
            .HasConstraintName("fk_lease_contracts_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        // -----------------------------------------------------------------------
        // Unique Constraints (Partial)
        // -----------------------------------------------------------------------
        builder.HasIndex(c => new { c.CompanyId, c.ContractNumber })
            .HasDatabaseName("uq_lease_contracts_company_contract_number")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(c => c.ApartmentId)
            .HasDatabaseName("uq_lease_contracts_one_active_per_apartment")
            .IsUnique()
            .HasFilter("status = 'active' AND deleted_at IS NULL");

        builder.HasIndex(c => c.PriorContractId)
            .HasDatabaseName("uq_lease_contracts_prior_contract_id")
            .IsUnique()
            .HasFilter("prior_contract_id IS NOT NULL AND deleted_at IS NULL");

        // -----------------------------------------------------------------------
        // Performance Indexes
        // -----------------------------------------------------------------------
        builder.HasIndex(c => new { c.ApartmentId, c.StartDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_lease_contracts_apartment_history")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(c => new { c.TenantId, c.StartDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_lease_contracts_tenant_history")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(c => new { c.CompanyId, c.Status })
            .HasDatabaseName("idx_lease_contracts_company_status")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(c => new { c.CompanyId, c.EndDate })
            .HasDatabaseName("idx_lease_contracts_expiration")
            .HasFilter("status = 'active' AND deleted_at IS NULL");

        // NOTE: MIGRATION SQL REQUIRED for idx_lease_contracts_contract_number_trgm
        // EF Core mapping for GIN trigram index must be added via raw SQL migration:
        // CREATE INDEX idx_lease_contracts_contract_number_trgm ON lease_contracts USING gin (contract_number gin_trgm_ops);

        // Global query filter
        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
