using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Infrastructure.Leasing.Configurations;

internal sealed class TenantVehicleConfiguration : IEntityTypeConfiguration<TenantVehicle>
{
    public void Configure(EntityTypeBuilder<TenantVehicle> builder)
    {
        builder.ToTable("tenant_vehicles");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(t => t.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(t => t.PlateNumber)
            .HasColumnName("plate_number")
            .HasColumnType("character varying(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.MakeModel)
            .HasColumnName("make_model")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Color)
            .HasColumnName("color")
            .HasColumnType("character varying(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(t => t.CreatedBy).HasColumnName("created_by").HasColumnType("uuid").IsRequired(false);
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by").HasColumnType("uuid").IsRequired(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by").HasColumnType("uuid").IsRequired(false);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>().WithMany().HasForeignKey(t => t.CreatedBy).HasConstraintName("fk_tenant_vehicles_users_created_by").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>().WithMany().HasForeignKey(t => t.UpdatedBy).HasConstraintName("fk_tenant_vehicles_users_updated_by").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>().WithMany().HasForeignKey(t => t.DeletedBy).HasConstraintName("fk_tenant_vehicles_users_deleted_by").OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Leasing.Tenant>()
            .WithMany(t => t.Vehicles)
            .HasForeignKey(v => new { v.CompanyId, v.TenantId })
            .HasPrincipalKey(t => new { t.CompanyId, t.Id })
            .HasConstraintName("fk_tenant_vehicles_tenants_company_tenant")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.CompanyId, v.PlateNumber })
            .HasDatabaseName("uq_tenant_vehicles_company_plate")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(v => new { v.CompanyId, v.TenantId })
            .HasDatabaseName("idx_tenant_vehicles_tenant");

        builder.HasQueryFilter(t => t.DeletedAt == null);

        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
    }
}
