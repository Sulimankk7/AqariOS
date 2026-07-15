using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PropertyOS.Infrastructure.Properties.Configurations;

public class ParkingAssignmentConfiguration : IEntityTypeConfiguration<Domain.Properties.ParkingAssignment>
{
    public void Configure(EntityTypeBuilder<Domain.Properties.ParkingAssignment> builder)
    {
        builder.ToTable("parking_assignments", t => 
        {
            t.HasCheckConstraint("chk_parking_assignments_dates", "assigned_to IS NULL OR assigned_to > assigned_from");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.ParkingSpotId)
            .HasColumnName("parking_spot_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.LeaseContractId)
            .HasColumnName("lease_contract_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.AssignedFrom)
            .HasColumnName("assigned_from")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(p => p.AssignedTo)
            .HasColumnName("assigned_to")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasColumnType("parking_assignment_status_enum")
            .HasDefaultValueSql("'active'")
            .IsRequired();

        // Audit columns
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(p => p.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        // Soft-delete columns
        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(p => p.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        // -----------------------------------------------------------------------
        // Composite FK: (company_id, parking_spot_id) -> parking_spots(company_id, id)
        // -----------------------------------------------------------------------
        builder.HasOne<Domain.Properties.ParkingSpot>()
            .WithMany()
            .HasForeignKey(p => new { p.CompanyId, p.ParkingSpotId })
            .HasPrincipalKey(ps => new { ps.CompanyId, ps.Id })
            .HasConstraintName("fk_parking_assignments_parking_spots_company_spot")
            .OnDelete(DeleteBehavior.Restrict);

        // -----------------------------------------------------------------------
        // Composite FK: (company_id, lease_contract_id) -> lease_contracts(company_id, id)
        // -----------------------------------------------------------------------
        builder.HasOne<Domain.Leasing.LeaseContract>()
            .WithMany()
            .HasForeignKey(p => new { p.CompanyId, p.LeaseContractId })
            .HasPrincipalKey(lc => new { lc.CompanyId, lc.Id })
            .HasConstraintName("fk_parking_assignments_lease_contracts_company_contract")
            .OnDelete(DeleteBehavior.Restrict);

        // -----------------------------------------------------------------------
        // FK: created_by / updated_by / deleted_by -> users(id) ON DELETE SET NULL
        // -----------------------------------------------------------------------
        builder.HasOne<Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .HasConstraintName("FK_parking_assignments_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(p => p.UpdatedBy)
            .HasConstraintName("FK_parking_assignments_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(p => p.DeletedBy)
            .HasConstraintName("FK_parking_assignments_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        // -----------------------------------------------------------------------
        // Partial unique constraint: uq_parking_assignments_active_spot
        // on (parking_spot_id) WHERE status = 'active' AND deleted_at IS NULL
        // -----------------------------------------------------------------------
        builder.HasIndex(p => p.ParkingSpotId)
            .HasDatabaseName("uq_parking_assignments_active_spot")
            .IsUnique()
            .HasFilter("status = 'active' AND deleted_at IS NULL");

        // -----------------------------------------------------------------------
        // Indexes
        // -----------------------------------------------------------------------
        builder.HasIndex(p => p.LeaseContractId)
            .HasDatabaseName("idx_parking_assignments_lease_contract_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
