using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Maintenance;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Infrastructure.Maintenance.Configurations;

internal sealed class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.ToTable("maintenance_requests", t =>
        {
            // Spec §8.1 — Check Constraints
            t.HasCheckConstraint(
                "chk_maintenance_requests_title_not_blank",
                "length(btrim(title)) > 0");

            t.HasCheckConstraint(
                "chk_maintenance_requests_request_date_not_future",
                "request_date <= CURRENT_DATE");

            t.HasCheckConstraint(
                "chk_maintenance_requests_closed_date_not_future",
                "closed_date IS NULL OR closed_date <= CURRENT_DATE");

            t.HasCheckConstraint(
                "chk_maintenance_requests_closed_date_not_before_request_date",
                "closed_date IS NULL OR closed_date >= request_date");

            // Terminal states (closed, cancelled) MUST have a closed_date.
            // Non-terminal states MUST NOT have a closed_date.
            // 'resolved' is deliberately excluded from terminal branch per spec §8.1.
            t.HasCheckConstraint(
                "chk_maintenance_requests_closed_date_status_consistency",
                "(status NOT IN ('closed', 'cancelled') AND closed_date IS NULL) OR (status IN ('closed', 'cancelled') AND closed_date IS NOT NULL)");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(r => r.BuildingId)
            .HasColumnName("building_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(r => r.ApartmentId)
            .HasColumnName("apartment_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(r => r.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(r => r.Title)
            .HasColumnName("title")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(r => r.Category)
            .HasColumnName("category")
            .HasColumnType("maintenance_category_enum")
            .IsRequired();

        builder.Property(r => r.Priority)
            .HasColumnName("priority")
            .HasColumnType("maintenance_priority_enum")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasColumnType("maintenance_status_enum")
            .HasDefaultValueSql("'open'")
            .IsRequired();

        builder.Property(r => r.RequestDate)
            .HasColumnName("request_date")
            .HasColumnType("date")
            .HasDefaultValueSql("CURRENT_DATE")
            .IsRequired();

        builder.Property(r => r.ClosedDate)
            .HasColumnName("closed_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(r => r.InternalNotes)
            .HasColumnName("internal_notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(r => r.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(r => r.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(r => r.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(r => r.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        // Optimistic concurrency via PostgreSQL system column xmin
        builder.Property(r => r.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        // ── Foreign Keys ──────────────────────────────────────────────────────

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(r => r.CompanyId)
            .HasConstraintName("fk_maintenance_requests_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Properties.Building>()
            .WithMany()
            .HasForeignKey(r => r.BuildingId)
            .HasConstraintName("fk_maintenance_requests_buildings_building_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Properties.Apartment>()
            .WithMany()
            .HasForeignKey(r => r.ApartmentId)
            .HasConstraintName("fk_maintenance_requests_apartments_apartment_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Leasing.Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .HasConstraintName("fk_maintenance_requests_tenants_tenant_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(r => r.CreatedBy)
            .HasConstraintName("fk_maintenance_requests_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(r => r.UpdatedBy)
            .HasConstraintName("fk_maintenance_requests_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(r => r.DeletedBy)
            .HasConstraintName("fk_maintenance_requests_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        // ── Navigation for child collections ─────────────────────────────────

        builder.HasMany(r => r.Attachments)
            .WithOne()
            .HasForeignKey(a => a.MaintenanceRequestId)
            .HasConstraintName("fk_maintenance_request_attachments_request_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Comments)
            .WithOne()
            .HasForeignKey(c => c.MaintenanceRequestId)
            .HasConstraintName("fk_maintenance_request_comments_request_id")
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes (spec §8.1) ──────────────────────────────────────────────

        // Primary worklist: company + status + date — covers dashboard tiles and status-filtered lists
        builder.HasIndex(r => new { r.CompanyId, r.Status, r.RequestDate })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_maintenance_requests_company_status_request_date")
            .HasFilter("deleted_at IS NULL");

        // Triage queue: partial index — only active statuses, priority-ordered
        builder.HasIndex(r => new { r.CompanyId, r.Priority, r.RequestDate })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_maintenance_requests_company_open_priority")
            .HasFilter("status IN ('open', 'in_progress', 'waiting') AND deleted_at IS NULL");

        // Building maintenance tab
        builder.HasIndex(r => new { r.BuildingId, r.RequestDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_maintenance_requests_building_request_date")
            .HasFilter("deleted_at IS NULL");

        // Apartment maintenance history — partial: common-area requests have no apartment
        builder.HasIndex(r => new { r.ApartmentId, r.RequestDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_maintenance_requests_apartment_request_date")
            .HasFilter("apartment_id IS NOT NULL AND deleted_at IS NULL");

        // Tenant portal "my requests" view — partial: common-area requests have no tenant
        builder.HasIndex(r => new { r.TenantId, r.RequestDate })
            .IsDescending(false, true)
            .HasDatabaseName("idx_maintenance_requests_tenant_request_date")
            .HasFilter("tenant_id IS NOT NULL AND deleted_at IS NULL");

        // Category dimension filtering
        builder.HasIndex(r => new { r.CompanyId, r.Category })
            .HasDatabaseName("idx_maintenance_requests_company_category")
            .HasFilter("deleted_at IS NULL");

        // Full-text search on title using GIN trigram index (spec §8.1)
        builder.HasIndex(r => r.Title)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("idx_maintenance_requests_title_trgm");

        // ── Ignore domain events for EF Core mapping ─────────────────────────
        builder.Ignore(r => r.DomainEvents);

        // ── Global query filter (soft delete) ────────────────────────────────
        builder.HasQueryFilter(r => r.DeletedAt == null);
    }
}
