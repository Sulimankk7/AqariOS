using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Maintenance;

namespace PropertyOS.Infrastructure.Maintenance.Configurations;

internal sealed class MaintenanceRequestAttachmentConfiguration
    : IEntityTypeConfiguration<MaintenanceRequestAttachment>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequestAttachment> builder)
    {
        builder.ToTable("maintenance_request_attachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.MaintenanceRequestId)
            .HasColumnName("maintenance_request_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.FileId)
            .HasColumnName("file_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(a => a.Description)
            .HasColumnName("description")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        // UploadedBy is deliberately kept separate from CreatedBy (spec §8.2 Business Rules):
        // the uploader and the row-inserting actor can diverge in tenant-portal / import scenarios.
        builder.Property(a => a.UploadedBy)
            .HasColumnName("uploaded_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(a => a.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(a => a.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(a => a.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(a => a.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        // ── Foreign Keys ──────────────────────────────────────────────────────

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(a => a.CompanyId)
            .HasConstraintName("fk_maintenance_request_attachments_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        // The parent FK is defined on the MaintenanceRequest side (HasMany/WithOne).
        // We still add a dedicated FK constraint name on this side for clarity.

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.UploadedBy)
            .HasConstraintName("fk_maintenance_request_attachments_users_uploaded_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.CreatedBy)
            .HasConstraintName("fk_maintenance_request_attachments_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.UpdatedBy)
            .HasConstraintName("fk_maintenance_request_attachments_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.DeletedBy)
            .HasConstraintName("fk_maintenance_request_attachments_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        // ── Unique Constraint (spec §8.2) ─────────────────────────────────────
        // Same file cannot be attached twice to the same request (active rows only).
        builder.HasIndex(a => new { a.MaintenanceRequestId, a.FileId })
            .IsUnique()
            .HasDatabaseName("uq_maintenance_request_attachments_request_file")
            .HasFilter("deleted_at IS NULL");

        // ── Index ─────────────────────────────────────────────────────────────
        // "List all attachments for this request" — the only realistic access pattern.
        builder.HasIndex(a => a.MaintenanceRequestId)
            .HasDatabaseName("idx_maintenance_request_attachments_request_id")
            .HasFilter("deleted_at IS NULL");

        // ── Global query filter (soft delete) ─────────────────────────────────
        builder.HasQueryFilter(a => a.DeletedAt == null);
    }
}
