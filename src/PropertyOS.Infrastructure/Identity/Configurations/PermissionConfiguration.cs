using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Infrastructure.Identity.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        
        builder.Property(e => e.Key).HasColumnName("key").HasColumnType("character varying(100)").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Module).HasColumnName("module").HasColumnType("character varying(50)").HasMaxLength(50).IsRequired();
        builder.Property(e => e.DescriptionEn).HasColumnName("description_en").HasColumnType("character varying(255)").HasMaxLength(255).IsRequired();
        builder.Property(e => e.DescriptionAr).HasColumnName("description_ar").HasColumnType("character varying(255)").HasMaxLength(255).IsRequired();
        builder.Property(e => e.IsDeprecated).HasColumnName("is_deprecated").HasColumnType("boolean").HasDefaultValue(false).IsRequired();
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(e => e.Key).HasDatabaseName("uq_permissions_key").IsUnique();
        builder.HasIndex(e => e.Module).HasDatabaseName("idx_permissions_module");
    }
}
