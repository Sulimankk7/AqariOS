using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Infrastructure.Leasing.Configurations;

internal sealed class TenantFamilyMemberConfiguration : IEntityTypeConfiguration<TenantFamilyMember>
{
    public void Configure(EntityTypeBuilder<TenantFamilyMember> builder)
    {
        builder.ToTable("tenant_family_members", t =>
        {
            t.HasCheckConstraint("chk_tenant_family_members_relationship", "btrim(relationship_type) <> ''");
            t.HasCheckConstraint("chk_tenant_family_members_age_bracket", "age_bracket IS NULL OR btrim(age_bracket) <> ''");
        });

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

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.RelationshipType)
            .HasColumnName("relationship_type")
            .HasColumnType("character varying(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.AgeBracket)
            .HasColumnName("age_bracket")
            .HasColumnType("character varying(30)")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(t => t.CreatedBy).HasColumnName("created_by").HasColumnType("uuid").IsRequired(false);
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by").HasColumnType("uuid").IsRequired(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by").HasColumnType("uuid").IsRequired(false);

        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>().WithMany().HasForeignKey(t => t.CreatedBy).HasConstraintName("fk_tenant_family_members_users_created_by").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>().WithMany().HasForeignKey(t => t.UpdatedBy).HasConstraintName("fk_tenant_family_members_users_updated_by").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<PropertyOS.Domain.Identity.Entities.User>().WithMany().HasForeignKey(t => t.DeletedBy).HasConstraintName("fk_tenant_family_members_users_deleted_by").OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PropertyOS.Domain.Leasing.Tenant>()
            .WithMany(t => t.FamilyMembers)
            .HasForeignKey(fm => new { fm.CompanyId, fm.TenantId })
            .HasPrincipalKey(t => new { t.CompanyId, t.Id })
            .HasConstraintName("fk_tenant_family_members_tenants_company_tenant")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(fm => new { fm.CompanyId, fm.TenantId })
            .HasDatabaseName("idx_tenant_family_members_tenant");

        builder.HasQueryFilter(t => t.DeletedAt == null);

        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
    }
}
