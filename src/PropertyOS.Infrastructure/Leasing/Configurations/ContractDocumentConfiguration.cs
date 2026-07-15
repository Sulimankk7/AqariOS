using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Infrastructure.Leasing.Configurations;

internal sealed class ContractDocumentConfiguration : IEntityTypeConfiguration<ContractDocument>
{
    public void Configure(EntityTypeBuilder<ContractDocument> builder)
    {
        builder.ToTable("contract_documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(d => d.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(d => d.LeaseContractId)
            .HasColumnName("lease_contract_id")
            .HasColumnType("uuid")
            .IsRequired();

        // Forward reference to file_storage table (raw UUID)
        builder.Property(d => d.FileId)
            .HasColumnName("file_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(d => d.DocumentType)
            .HasColumnName("document_type")
            .HasColumnType("contract_document_type_enum")
            .HasDefaultValueSql("'other'")
            .IsRequired();

        builder.Property(d => d.Description)
            .HasColumnName("description")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(d => d.UploadedBy)
            .HasColumnName("uploaded_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(d => d.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(d => d.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        // -----------------------------------------------------------------------
        // Foreign Keys
        // -----------------------------------------------------------------------
        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(d => d.CompanyId)
            .HasConstraintName("fk_contract_documents_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LeaseContract>()
            .WithMany()
            .HasForeignKey(d => new { d.CompanyId, d.LeaseContractId })
            .HasPrincipalKey(c => new { c.CompanyId, c.Id })
            .HasConstraintName("fk_contract_documents_lease_contracts_company_contract")
            .OnDelete(DeleteBehavior.Restrict);

        // Note: Missing FK to file_storage. It physically does not exist yet.
        // MIGRATION SQL / FUTURE MODULE REQUIRED for:
        // ADD CONSTRAINT fk_contract_documents_file_storage_file_id FOREIGN KEY (file_id) REFERENCES file_storage(id) ON DELETE RESTRICT;

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(d => d.UploadedBy)
            .HasConstraintName("fk_contract_documents_users_uploaded_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(d => d.DeletedBy)
            .HasConstraintName("fk_contract_documents_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        // -----------------------------------------------------------------------
        // Unique Constraints (Partial)
        // -----------------------------------------------------------------------
        builder.HasIndex(d => new { d.LeaseContractId, d.FileId })
            .HasDatabaseName("uq_contract_documents_contract_file")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        // -----------------------------------------------------------------------
        // Indexes
        // -----------------------------------------------------------------------
        builder.HasIndex(d => d.LeaseContractId)
            .HasDatabaseName("idx_contract_documents_lease_contract_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(d => new { d.LeaseContractId, d.DocumentType })
            .HasDatabaseName("idx_contract_documents_type")
            .HasFilter("deleted_at IS NULL");

        // Global query filter
        builder.HasQueryFilter(d => d.DeletedAt == null);
    }
}
