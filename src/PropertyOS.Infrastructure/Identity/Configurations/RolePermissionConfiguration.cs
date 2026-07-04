using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Infrastructure.Identity.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        
        builder.Property(e => e.RoleId).HasColumnName("role_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.PermissionId).HasColumnName("permission_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.GrantedAt).HasColumnName("granted_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.GrantedBy).HasColumnName("granted_by").HasColumnType("uuid").IsRequired(false);

        builder.HasIndex(e => new { e.RoleId, e.PermissionId }).HasDatabaseName("uq_role_permissions_role_permission").IsUnique();

        builder.HasOne(e => e.Role).WithMany(r => r.RolePermissions).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Permission).WithMany(p => p.RolePermissions).HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.GrantedByUser).WithMany().HasForeignKey(e => e.GrantedBy).OnDelete(DeleteBehavior.SetNull);
    }
}
