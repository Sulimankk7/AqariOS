using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Infrastructure.Identity.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", t =>
        {
            t.HasCheckConstraint("chk_roles_is_system_consistency", "(is_system = true AND company_id IS NULL) OR (is_system = false AND company_id IS NOT NULL)");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        
        builder.Property(e => e.CompanyId).HasColumnName("company_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.Code).HasColumnName("code").HasColumnType("character varying(50)").HasMaxLength(50).IsRequired();
        builder.Property(e => e.NameEn).HasColumnName("name_en").HasColumnType("character varying(100)").HasMaxLength(100).IsRequired();
        builder.Property(e => e.NameAr).HasColumnName("name_ar").HasColumnType("character varying(100)").HasMaxLength(100).IsRequired();
        builder.Property(e => e.IsSystem).HasColumnName("is_system").HasColumnType("boolean").HasDefaultValue(false).IsRequired();
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasColumnType("uuid").IsRequired(false);

        builder.HasIndex(e => e.Code).HasDatabaseName("uq_roles_system_code").IsUnique(); 
        builder.HasIndex(e => new { e.CompanyId, e.Code }).HasDatabaseName("uq_roles_company_code").IsUnique(); 
        builder.HasIndex(e => e.CompanyId).HasDatabaseName("idx_roles_company_id"); 

        builder.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => e.DeletedAt == null);

        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
    }
}
