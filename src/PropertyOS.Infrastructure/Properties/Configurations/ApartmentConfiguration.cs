using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Infrastructure.Properties.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Apartment"/> entity.
/// Maps to the <c>apartments</c> table (Module 4 §4.4).
///
/// KEY HIERARCHY INTEGRITY:
///   The composite FK (company_id, building_id, floor_id) → floors(company_id, building_id, id)
///   is the central hierarchy contract. It proves atomically that:
///     • apartment.company_id = floor.company_id
///     • apartment.building_id = floor.building_id
///     • apartment.floor_id identifies that exact floor
///   This replaces three independent UUID FKs and prevents the cross-company
///   floor misattribution scenario.
///
/// OCCUPANCY STATUS:
///   Created as a physical column with default 'vacant'. Write-back trigger
///   (AFTER INSERT OR UPDATE OF status ON lease_contracts) is deferred to Module 5.
///
/// SCALE: 2,000,000+ rows. No partitioning — composite indexes are the mechanism.
/// </summary>
internal sealed class ApartmentConfiguration : IEntityTypeConfiguration<Apartment>
{
    public void Configure(EntityTypeBuilder<Apartment> builder)
    {
        // -----------------------------------------------------------------------
        // Table and check constraints
        // -----------------------------------------------------------------------
        builder.ToTable("apartments", t =>
        {
            t.HasCheckConstraint("chk_apartments_area_positive", "area_sqm > 0");
            t.HasCheckConstraint("chk_apartments_bedrooms_nonneg", "bedrooms >= 0");
            t.HasCheckConstraint("chk_apartments_bathrooms_nonneg", "bathrooms >= 0");
            t.HasCheckConstraint(
                "chk_apartments_base_rent_positive",
                "base_rent_amount IS NULL OR base_rent_amount > 0");
            t.HasCheckConstraint(
                "chk_apartments_external_owner_required",
                "(ownership_status = 'company_owned') OR (external_owner_name IS NOT NULL)");
        });

        // -----------------------------------------------------------------------
        // Primary Key
        // -----------------------------------------------------------------------
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        // -----------------------------------------------------------------------
        // Composite hierarchy key columns
        // -----------------------------------------------------------------------

        builder.Property(a => a.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.BuildingId)
            .HasColumnName("building_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.FloorId)
            .HasColumnName("floor_id")
            .HasColumnType("uuid")
            .IsRequired();

        // -----------------------------------------------------------------------
        // Unit identity
        // -----------------------------------------------------------------------

        builder.Property(a => a.UnitNumber)
            .HasColumnName("unit_number")
            .HasColumnType("character varying(20)")
            .HasMaxLength(20)
            .IsRequired();

        // -----------------------------------------------------------------------
        // Ownership
        // -----------------------------------------------------------------------

        builder.Property(a => a.OwnershipStatus)
            .HasColumnName("ownership_status")
            .HasColumnType("ownership_status_enum")
            .HasDefaultValueSql("'company_owned'")
            .IsRequired();

        builder.Property(a => a.ExternalOwnerName)
            .HasColumnName("external_owner_name")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(a => a.ExternalOwnerPhone)
            .HasColumnName("external_owner_phone")
            .HasColumnType("character varying(20)")
            .HasMaxLength(20)
            .IsRequired(false);

        // -----------------------------------------------------------------------
        // Occupancy status — trigger-maintained cache (Module 5 owns the trigger)
        // -----------------------------------------------------------------------

        builder.Property(a => a.OccupancyStatus)
            .HasColumnName("occupancy_status")
            .HasColumnType("occupancy_status_enum")
            .HasDefaultValueSql("'vacant'")
            .IsRequired();

        // -----------------------------------------------------------------------
        // Physical attributes
        // -----------------------------------------------------------------------

        builder.Property(a => a.AreaSqm)
            .HasColumnName("area_sqm")
            .HasColumnType("numeric(7,2)")
            .IsRequired();

        builder.Property(a => a.Bedrooms)
            .HasColumnName("bedrooms")
            .HasColumnType("smallint")
            .HasDefaultValue((short)0)
            .IsRequired();

        builder.Property(a => a.Bathrooms)
            .HasColumnName("bathrooms")
            .HasColumnType("smallint")
            .HasDefaultValue((short)0)
            .IsRequired();

        // -----------------------------------------------------------------------
        // Pricing
        // -----------------------------------------------------------------------

        builder.Property(a => a.BaseRentAmount)
            .HasColumnName("base_rent_amount")
            .HasColumnType("numeric(12,3)")
            .IsRequired(false);

        builder.Property(a => a.BaseRentCurrency)
            .HasColumnName("base_rent_currency")
            .HasColumnType("character(3)")
            .HasMaxLength(3)
            .HasDefaultValue("JOD")
            .IsRequired();

        builder.Property(a => a.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired();

        // -----------------------------------------------------------------------
        // Audit columns
        // -----------------------------------------------------------------------

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

        // -----------------------------------------------------------------------
        // Soft-delete columns
        // -----------------------------------------------------------------------

        builder.Property(a => a.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(a => a.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);



        // -----------------------------------------------------------------------
        // COMPOSITE FK: (company_id, building_id, floor_id)
        //     → floors(company_id, building_id, id)
        //
        // This references the candidate key UNIQUE(company_id, building_id, id) on floors.
        // It atomically proves all three of:
        //   1. apartment.company_id = floor.company_id
        //   2. apartment.building_id = floor.building_id
        //   3. apartment.floor_id resolves to that exact floor
        // Application validation is NOT sufficient for this invariant.
        // RLS is NOT referential integrity. This FK is.
        // -----------------------------------------------------------------------
        builder.HasOne<Floor>()
            .WithMany(f => f.Apartments)
            .HasForeignKey(a => new { a.CompanyId, a.BuildingId, a.FloorId })
            .HasPrincipalKey(f => new { f.CompanyId, f.BuildingId, f.Id })
            .HasConstraintName("fk_apartments_floors_company_building_floor")
            .OnDelete(DeleteBehavior.Restrict);

        // -----------------------------------------------------------------------
        // Candidate key: UNIQUE(company_id, building_id, id)
        // Required by the composite FK from parking_spots:
        //   parking_spots(company_id, building_id, default_apartment_id)
        //       → apartments(company_id, building_id, id)
        // -----------------------------------------------------------------------
        builder.HasAlternateKey(a => new { a.CompanyId, a.BuildingId, a.Id })
            .HasName("uq_apartments_company_building_id");

        // -----------------------------------------------------------------------
        // FK: created_by / updated_by / deleted_by → users(id) ON DELETE SET NULL
        // -----------------------------------------------------------------------
        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.CreatedBy)
            .HasConstraintName("FK_apartments_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.UpdatedBy)
            .HasConstraintName("FK_apartments_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.DeletedBy)
            .HasConstraintName("FK_apartments_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        // -----------------------------------------------------------------------
        // Partial unique: uq_apartments_building_unit_number
        // Scoped to building_id — a unit number must be unique within one building
        // regardless of which floor it is attached to.
        // WHERE clause applied in migration raw SQL.
        // -----------------------------------------------------------------------
        builder.HasIndex(a => new { a.BuildingId, a.UnitNumber })
            .HasDatabaseName("uq_apartments_building_unit_number")
            .IsUnique();
        // NOTE: migration drops and recreates as:
        //   CREATE UNIQUE INDEX uq_apartments_building_unit_number
        //     ON apartments(building_id, unit_number)
        //     WHERE deleted_at IS NULL;

        // -----------------------------------------------------------------------
        // Indexes — approved hot paths from §4.4
        //
        // idx_apartments_building_id: (building_id, floor_id) WHERE deleted_at IS NULL
        // idx_apartments_company_occupancy: (company_id, occupancy_status) WHERE deleted_at IS NULL
        // idx_apartments_company_occupancy_bedrooms: (company_id, occupancy_status, bedrooms)
        //   WHERE deleted_at IS NULL AND occupancy_status = 'vacant'
        // idx_apartments_floor_id: (floor_id) WHERE deleted_at IS NULL
        //
        // All created as raw SQL partial indexes in the migration.
        // EF metadata registrations below (for discovery only):
        // -----------------------------------------------------------------------
        builder.HasIndex(a => new { a.BuildingId, a.FloorId })
            .HasDatabaseName("idx_apartments_building_id");

        builder.HasIndex(a => new { a.CompanyId, a.OccupancyStatus })
            .HasDatabaseName("idx_apartments_company_occupancy");

        builder.HasIndex(a => new { a.CompanyId, a.OccupancyStatus, a.Bedrooms })
            .HasDatabaseName("idx_apartments_company_occupancy_bedrooms");

        builder.HasIndex(a => a.FloorId)
            .HasDatabaseName("idx_apartments_floor_id");
        // NOTE: all four replaced by partial indexes in migration raw SQL.

        // -----------------------------------------------------------------------
        // Global query filter: soft-delete only
        // -----------------------------------------------------------------------
        builder.HasQueryFilter(a => a.DeletedAt == null);

        // -----------------------------------------------------------------------
        // Concurrency token
        // -----------------------------------------------------------------------
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
    }
}
