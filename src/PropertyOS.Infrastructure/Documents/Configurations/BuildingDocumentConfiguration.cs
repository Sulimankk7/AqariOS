using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Documents.Entities;

namespace PropertyOS.Infrastructure.Documents.Configurations;

internal sealed class BuildingDocumentConfiguration : IEntityTypeConfiguration<BuildingDocument>
{
    public void Configure(EntityTypeBuilder<BuildingDocument> builder)
    {
        builder.ToTable("building_documents", t =>
        {
            t.HasCheckConstraint("chk_building_documents_name_not_blank", "length(btrim(document_name)) > 0");
            t.HasCheckConstraint("chk_building_documents_issue_date_not_future", "issue_date IS NULL OR issue_date <= CURRENT_DATE");
            t.HasCheckConstraint("chk_building_documents_expiry_after_issue", "expiry_date IS NULL OR issue_date IS NULL OR expiry_date > issue_date");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()");

        builder.Property(e => e.CompanyId).HasColumnName("company_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.BuildingId).HasColumnName("building_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.CategoryId).HasColumnName("category_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.FileId).HasColumnName("file_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.DocumentName).HasColumnName("document_name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(e => e.IssueDate).HasColumnName("issue_date").HasColumnType("date");
        builder.Property(e => e.ExpiryDate).HasColumnName("expiry_date").HasColumnType("date");
        builder.Property(e => e.IsConfidential).HasColumnName("is_confidential").HasColumnType("boolean").HasDefaultValue(false).IsRequired();
        builder.Property(e => e.UploadedBy).HasColumnName("uploaded_by").HasColumnType("uuid");

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasColumnType("uuid");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasColumnType("uuid");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasColumnType("uuid");

        builder.HasQueryFilter(e => e.DeletedAt == null);

        // Unique constraint: uq_building_documents_building_file
        builder.HasIndex(e => new { e.BuildingId, e.FileId })
            .HasDatabaseName("uq_building_documents_building_file")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        // Indexes
        builder.HasIndex(e => new { e.BuildingId, e.CreatedAt })
            .HasDatabaseName("idx_building_documents_building_created")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(e => new { e.BuildingId, e.CategoryId })
            .HasDatabaseName("idx_building_documents_building_category")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(e => new { e.CompanyId, e.CategoryId })
            .HasDatabaseName("idx_building_documents_company_category")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(e => new { e.CompanyId, e.ExpiryDate })
            .HasDatabaseName("idx_building_documents_expiring")
            .HasFilter("expiry_date IS NOT NULL AND deleted_at IS NULL");

        // Trigram index on document_name
        builder.HasIndex(e => e.DocumentName)
            .HasDatabaseName("idx_building_documents_document_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        // Foreign keys
        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Properties.Building>()
            .WithMany()
            .HasForeignKey(e => e.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DocumentCategory>()
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Files.Entities.FileStorage>()
            .WithMany()
            .HasForeignKey(e => e.FileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(e => e.UploadedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(e => e.UpdatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(e => e.DeletedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
