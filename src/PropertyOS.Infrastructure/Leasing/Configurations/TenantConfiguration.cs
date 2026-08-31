using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Infrastructure.Leasing.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

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

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.NationalId)
            .HasColumnName("national_id")
            .HasColumnType("character varying(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Phone)
            .HasColumnName("phone")
            .HasColumnType("character varying(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Email)
            .HasColumnName("email")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(t => t.Occupation)
            .HasColumnName("occupation")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(t => t.Employer)
            .HasColumnName("employer")
            .HasColumnType("character varying(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(t => t.CreatedBy).HasColumnName("created_by").HasColumnType("uuid").IsRequired(false);
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by").HasColumnType("uuid").IsRequired(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by").HasColumnType("uuid").IsRequired(false);

        builder.HasOne<PropertyOS.Domain.Companies.Company>()
            .WithMany()
            .HasForeignKey(t => t.CompanyId)
            .HasConstraintName("fk_tenants_companies")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .HasConstraintName("fk_tenants_users")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>().WithMany().HasForeignKey(t => t.CreatedBy).HasConstraintName("fk_tenants_users_created_by").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>().WithMany().HasForeignKey(t => t.UpdatedBy).HasConstraintName("fk_tenants_users_updated_by").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>().WithMany().HasForeignKey(t => t.DeletedBy).HasConstraintName("fk_tenants_users_deleted_by").OnDelete(DeleteBehavior.SetNull);

        builder.HasAlternateKey(t => new { t.CompanyId, t.Id })
            .HasName("uq_tenants_company_id");

        builder.HasIndex(t => new { t.CompanyId, t.NationalId })
            .HasDatabaseName("uq_tenants_company_national_id")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(t => new { t.CompanyId, t.Phone })
            .HasDatabaseName("idx_tenants_company_phone")
            .HasFilter("deleted_at IS NULL");

        // Global active Tenant contact-phone uniqueness. The pre-migration data audit
        // confirmed that active canonical values contain no invalid rows or collisions.
        builder.HasIndex(t => t.Phone)
            .HasDatabaseName("uq_tenants_phone_active")
            .IsUnique()
            .HasFilter("phone IS NOT NULL AND deleted_at IS NULL");

        builder.HasIndex(t => new { t.CompanyId, t.Email })
            .HasDatabaseName("idx_tenants_company_email")
            .HasFilter("deleted_at IS NULL");

        builder.HasQueryFilter(t => t.DeletedAt == null);

        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
    }
}
