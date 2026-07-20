using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Maintenance;

namespace PropertyOS.Infrastructure.Maintenance.Configurations;

internal sealed class MaintenanceRequestCommentConfiguration
    : IEntityTypeConfiguration<MaintenanceRequestComment>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequestComment> builder)
    {
        builder.ToTable("maintenance_request_comments", t =>
        {
            t.HasCheckConstraint(
                "chk_maintenance_request_comments_text_not_blank",
                "length(btrim(comment_text)) > 0");
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

        builder.Property(c => c.MaintenanceRequestId)
            .HasColumnName("maintenance_request_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(c => c.CommentText)
            .HasColumnName("comment_text")
            .HasColumnType("text")
            .IsRequired();

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

        // Optimistic concurrency via xmin system column
        builder.Property(c => c.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        // ── Foreign Keys ──────────────────────────────────────────────────────

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(c => c.CompanyId)
            .HasConstraintName("fk_maintenance_request_comments_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(c => c.CreatedBy)
            .HasConstraintName("fk_maintenance_request_comments_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(c => c.UpdatedBy)
            .HasConstraintName("fk_maintenance_request_comments_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(c => c.DeletedBy)
            .HasConstraintName("fk_maintenance_request_comments_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        // ── Index ─────────────────────────────────────────────────────────────
        // "List comments for a request, sorted by creation date"
        builder.HasIndex(c => new { c.MaintenanceRequestId, c.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_maintenance_request_comments_request_created_at")
            .HasFilter("deleted_at IS NULL");

        // ── Global query filter (soft delete) ─────────────────────────────────
        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
