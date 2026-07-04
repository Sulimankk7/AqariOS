using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Infrastructure.Properties.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="BuildingAddress"/> entity.
/// Maps to the <c>building_addresses</c> table (Module 4 §4.2).
///
/// Key design points:
///   • Composite FK (company_id, building_id) → buildings(company_id, id) is
///     implemented via the HasForeignKey on both columns referencing the candidate
///     key on buildings. This physically prevents an address from referencing a
///     building from another company.
///   • 1:1 uniqueness (uq_building_addresses_building_id) enforced here.
///   • GIN trigram index on area is created in migration raw SQL (requires
///     pg_trgm extension which is already present from Module 1).
/// </summary>
internal sealed class BuildingAddressConfiguration : IEntityTypeConfiguration<BuildingAddress>
{
    public void Configure(EntityTypeBuilder<BuildingAddress> builder)
    {
        // -----------------------------------------------------------------------
        // Table — no CHECK constraints beyond enum typing (see §4.2)
        // -----------------------------------------------------------------------
        builder.ToTable("building_addresses");

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
        // Hierarchy FK columns — both required for composite FK integrity
        // -----------------------------------------------------------------------

        builder.Property(a => a.BuildingId)
            .HasColumnName("building_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(a => a.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        // -----------------------------------------------------------------------
        // Address columns
        // -----------------------------------------------------------------------

        builder.Property(a => a.Governorate)
            .HasColumnName("governorate")
            .HasColumnType("governorate_enum")
            .IsRequired();

        builder.Property(a => a.District)
            .HasColumnName("district")
            .HasColumnType("character varying(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.Area)
            .HasColumnName("area")
            .HasColumnType("character varying(150)")
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(a => a.StreetName)
            .HasColumnName("street_name")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(a => a.BuildingPlateNumber)
            .HasColumnName("building_plate_number")
            .HasColumnType("character varying(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(a => a.NearestLandmark)
            .HasColumnName("nearest_landmark")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(a => a.PostalCode)
            .HasColumnName("postal_code")
            .HasColumnType("character varying(20)")
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(a => a.FullAddressText)
            .HasColumnName("full_address_text")
            .HasColumnType("text")
            .IsRequired(false);

        // -----------------------------------------------------------------------
        // Audit columns — created_at / updated_at only (no actor attribution)
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



        // -----------------------------------------------------------------------
        // Composite FK: (company_id, building_id) → buildings(company_id, id)
        //
        // This is the physical hierarchy integrity mechanism. The candidate key
        // UNIQUE(company_id, id) on buildings is the principal key referenced here.
        // EF Core expresses this via HasPrincipalKey referencing the alternate key.
        //
        // Relationship principal already declared in BuildingConfiguration
        // (HasOne<BuildingAddress> WithOne HasForeignKey<BuildingAddress>(a => a.BuildingId)).
        // That relationship covers the standard building_id FK. The composite
        // tenant-isolation FK is expressed here via raw SQL in the migration because
        // EF Core does not support multi-column FKs where one column (company_id) is
        // also independently mapped as a regular FK. The raw SQL migration handles:
        //   ALTER TABLE building_addresses ADD CONSTRAINT
        //     fk_building_addresses_buildings_company_building
        //     FOREIGN KEY (company_id, building_id)
        //     REFERENCES buildings(company_id, id);
        // This is documented in the migration. The EF model does not duplicate it
        // to avoid relationship tracking conflicts.
        // -----------------------------------------------------------------------

        // -----------------------------------------------------------------------
        // Unique constraint: 1:1 cardinality on building_id
        // -----------------------------------------------------------------------
        builder.HasIndex(a => a.BuildingId)
            .HasDatabaseName("uq_building_addresses_building_id")
            .IsUnique();

        // -----------------------------------------------------------------------
        // Index: (company_id, governorate, district) — location filter hot path
        // Created in raw SQL in the migration to control column order precisely.
        // EF metadata only:
        // -----------------------------------------------------------------------
        builder.HasIndex(a => new { a.CompanyId, a.Governorate, a.District })
            .HasDatabaseName("idx_building_addresses_governorate_district");
        // NOTE: GIN trigram index on area (idx_building_addresses_area_trgm)
        // is created in raw SQL in the migration — not expressible via EF fluent API.
    }
}
