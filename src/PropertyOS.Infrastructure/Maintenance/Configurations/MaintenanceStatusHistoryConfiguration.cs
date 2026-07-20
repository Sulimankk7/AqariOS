using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Maintenance;

namespace PropertyOS.Infrastructure.Maintenance.Configurations;

internal sealed class MaintenanceStatusHistoryConfiguration
    : IEntityTypeConfiguration<MaintenanceStatusHistory>
{
    public void Configure(EntityTypeBuilder<MaintenanceStatusHistory> builder)
    {
        builder.ToTable("maintenance_status_history", t =>
        {
            t.HasCheckConstraint(
                "chk_maintenance_status_history_no_noop",
                "previous_status IS NULL OR previous_status != new_status");
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

        builder.Property(h => h.MaintenanceRequestId)
            .HasColumnName("maintenance_request_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(h => h.PreviousStatus)
            .HasColumnName("previous_status")
            .HasColumnType("maintenance_status_enum")
            .IsRequired(false);

        builder.Property(h => h.NewStatus)
            .HasColumnName("new_status")
            .HasColumnType("maintenance_status_enum")
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

        // ── Foreign Keys ──────────────────────────────────────────────────────

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(h => h.CompanyId)
            .HasConstraintName("fk_maintenance_status_history_companies_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Maintenance.MaintenanceRequest>()
            .WithMany()
            .HasForeignKey(h => h.MaintenanceRequestId)
            .HasConstraintName("fk_maintenance_status_history_requests_request_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(h => h.ChangedBy)
            .HasConstraintName("fk_maintenance_status_history_users_changed_by")
            .OnDelete(DeleteBehavior.SetNull);

        // ── Indexes (spec §8.4) ──────────────────────────────────────────────
        // "List status timeline for a request"
        builder.HasIndex(h => new { h.MaintenanceRequestId, h.ChangedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_maintenance_status_history_request_changed_at");

        // Dashboard/analytical transition stats
        builder.HasIndex(h => new { h.CompanyId, h.NewStatus, h.ChangedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_maintenance_status_history_company_new_status");
    }
}
