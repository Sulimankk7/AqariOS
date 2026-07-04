using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PropertyOS.Infrastructure.Properties.Configurations;

public class ParkingSpotConfiguration : IEntityTypeConfiguration<Domain.Properties.ParkingSpot>
{
    public void Configure(EntityTypeBuilder<Domain.Properties.ParkingSpot> builder)
    {
        builder.ToTable("parking_spots");

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

        builder.Property(p => p.BuildingId)
            .HasColumnName("building_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(p => p.DefaultApartmentId)
            .HasColumnName("default_apartment_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(p => p.SpotCode)
            .HasColumnName("spot_code")
            .HasColumnType("character varying(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.ParkingType)
            .HasColumnName("parking_type")
            .HasColumnType("parking_type_enum")
            .HasDefaultValueSql("'standard'")
            .IsRequired();

        builder.Property(p => p.LocationDescription)
            .HasColumnName("location_description")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .HasDefaultValue(true)
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
        // Candidate key: UNIQUE(company_id, id)
        // Required by parking_assignments -> parking_spots
        // -----------------------------------------------------------------------
        builder.HasAlternateKey(p => new { p.CompanyId, p.Id })
            .HasName("uq_parking_spots_company_id");

        // -----------------------------------------------------------------------
        // Composite FK: (company_id, building_id) -> buildings(company_id, id)
        // -----------------------------------------------------------------------
        builder.HasOne<Domain.Properties.Building>()
            .WithMany()
            .HasForeignKey(p => new { p.CompanyId, p.BuildingId })
            .HasPrincipalKey(b => new { b.CompanyId, b.Id })
            .HasConstraintName("fk_parking_spots_buildings_company_building")
            .OnDelete(DeleteBehavior.Restrict);

        // -----------------------------------------------------------------------
        // Composite FK: (company_id, building_id, default_apartment_id) -> apartments(company_id, building_id, id)
        // NOTE: EF Core does not correctly handle composite foreign keys where one
        // of the dependent columns is nullable but the others are not, particularly
        // regarding the SET NULL ON DELETE constraint generation.
        // We configure it logically here for navigation, but the actual physical
        // constraint with column-list SET NULL is enforced via raw SQL in the migration:
        //
        // ALTER TABLE parking_spots ADD CONSTRAINT fk_parking_spots_apartments
        // FOREIGN KEY (company_id, building_id, default_apartment_id)
        // REFERENCES apartments(company_id, building_id, id) ON DELETE SET NULL (default_apartment_id);
        // -----------------------------------------------------------------------
        builder.HasOne<Domain.Properties.Apartment>()
            .WithMany()
            .HasForeignKey(p => new { p.CompanyId, p.BuildingId, p.DefaultApartmentId })
            .HasPrincipalKey(a => new { a.CompanyId, a.BuildingId, a.Id })
            .HasConstraintName("fk_parking_spots_apartments_company_building_apartment")
            .OnDelete(DeleteBehavior.SetNull);

        // -----------------------------------------------------------------------
        // FK: created_by / updated_by / deleted_by -> users(id) ON DELETE SET NULL
        // -----------------------------------------------------------------------
        builder.HasOne<Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .HasConstraintName("FK_parking_spots_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(p => p.UpdatedBy)
            .HasConstraintName("FK_parking_spots_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(p => p.DeletedBy)
            .HasConstraintName("FK_parking_spots_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        // -----------------------------------------------------------------------
        // Unique constraint: uq_parking_spots_building_spot_code
        // WHERE deleted_at IS NULL
        // -----------------------------------------------------------------------
        builder.HasIndex(p => new { p.BuildingId, p.SpotCode })
            .HasDatabaseName("uq_parking_spots_building_spot_code")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        // -----------------------------------------------------------------------
        // Indexes
        // -----------------------------------------------------------------------
        builder.HasIndex(p => p.BuildingId)
            .HasDatabaseName("idx_parking_spots_building_id")
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(p => p.DefaultApartmentId)
            .HasDatabaseName("idx_parking_spots_default_apartment_id")
            .HasFilter("default_apartment_id IS NOT NULL AND deleted_at IS NULL");

        builder.HasIndex(p => new { p.CompanyId, p.IsActive })
            .HasDatabaseName("idx_parking_spots_company_active")
            .HasFilter("deleted_at IS NULL");

        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
