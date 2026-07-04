using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Infrastructure.Properties.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Floor"/> entity.
/// Maps to the <c>floors</c> table (Module 4 §4.3).
///
/// REDUNDANT INDEX REPORT:
///   The candidate key UNIQUE(company_id, building_id, id) on floors produces an
///   EF-managed index named "uq_floors_company_building_id". The partial index
///   idx_floors_building_id on (building_id, floor_number) WHERE deleted_at IS NULL
///   is created in raw SQL in the migration. No EF relationship causes a standalone
///   building_id index to be auto-created (EF creates FK indexes for referencing
///   side only when the relationship is declared; that index is replaced by the
///   partial index in raw SQL). No duplicate index risk.
///
///   A standalone company_id index on floors is deliberately NOT created per §4.3:
///   floors are not queried at the company-wide level; company_id exists only for
///   RLS enforcement.
/// </summary>
internal sealed class FloorConfiguration : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> builder)
    {
        // -----------------------------------------------------------------------
        // Table — no CHECK constraints (see §4.3 for rationale)
        // -----------------------------------------------------------------------
        builder.ToTable("floors");

        // -----------------------------------------------------------------------
        // Primary Key
        // -----------------------------------------------------------------------
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        // -----------------------------------------------------------------------
        // Hierarchy key columns
        // -----------------------------------------------------------------------

        builder.Property(f => f.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(f => f.BuildingId)
            .HasColumnName("building_id")
            .HasColumnType("uuid")
            .IsRequired();

        // -----------------------------------------------------------------------
        // Floor identity columns
        // -----------------------------------------------------------------------

        builder.Property(f => f.FloorNumber)
            .HasColumnName("floor_number")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(f => f.FloorLabel)
            .HasColumnName("floor_label")
            .HasColumnType("character varying(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.FloorType)
            .HasColumnName("floor_type")
            .HasColumnType("floor_type_enum")
            .HasDefaultValueSql("'regular'")
            .IsRequired();

        builder.Property(f => f.ApartmentsCount)
            .HasColumnName("apartments_count")
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .IsRequired();

        // -----------------------------------------------------------------------
        // Audit columns
        // -----------------------------------------------------------------------

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(f => f.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(f => f.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        // -----------------------------------------------------------------------
        // Soft-delete columns
        // -----------------------------------------------------------------------

        builder.Property(f => f.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(f => f.DeletedBy)
            .HasColumnName("deleted_by")
            .HasColumnType("uuid")
            .IsRequired(false);



        // -----------------------------------------------------------------------
        // FK: created_by / updated_by / deleted_by → users(id) ON DELETE SET NULL
        // -----------------------------------------------------------------------
        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(f => f.CreatedBy)
            .HasConstraintName("FK_floors_users_created_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(f => f.UpdatedBy)
            .HasConstraintName("FK_floors_users_updated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(f => f.DeletedBy)
            .HasConstraintName("FK_floors_users_deleted_by")
            .OnDelete(DeleteBehavior.SetNull);

        // -----------------------------------------------------------------------
        // Candidate key: UNIQUE(company_id, building_id, id)
        // Required by the composite FK from apartments:
        //   apartments(company_id, building_id, floor_id)
        //       → floors(company_id, building_id, id)
        // -----------------------------------------------------------------------
        builder.HasAlternateKey(f => new { f.CompanyId, f.BuildingId, f.Id })
            .HasName("uq_floors_company_building_id");

        // -----------------------------------------------------------------------
        // Partial unique: uq_floors_building_floor_number
        // WHERE deleted_at IS NULL — prevents two floors with the same sort position.
        // EF metadata only; WHERE clause applied in migration raw SQL.
        // -----------------------------------------------------------------------
        builder.HasIndex(f => new { f.BuildingId, f.FloorNumber })
            .HasDatabaseName("uq_floors_building_floor_number")
            .IsUnique();
        // NOTE: migration drops and recreates as:
        //   CREATE UNIQUE INDEX uq_floors_building_floor_number
        //     ON floors(building_id, floor_number)
        //     WHERE deleted_at IS NULL;

        // -----------------------------------------------------------------------
        // Global query filter: soft-delete only (RLS handles tenant isolation)
        // -----------------------------------------------------------------------
        builder.HasQueryFilter(f => f.DeletedAt == null);

        // -----------------------------------------------------------------------
        // Concurrency token
        // -----------------------------------------------------------------------
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
    }
}
