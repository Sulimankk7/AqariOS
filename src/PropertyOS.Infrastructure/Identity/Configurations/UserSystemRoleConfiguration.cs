using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Infrastructure.Identity.Configurations;

internal sealed class UserSystemRoleConfiguration : IEntityTypeConfiguration<UserSystemRole>
{
    public void Configure(EntityTypeBuilder<UserSystemRole> builder)
    {
        builder.ToTable("user_system_roles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        builder.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.RoleId).HasColumnName("role_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.GrantedAt).HasColumnName("granted_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.GrantedBy).HasColumnName("granted_by").HasColumnType("uuid");

        builder.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique().HasDatabaseName("uq_user_system_roles_user_role");
        builder.HasIndex(x => x.RoleId).HasDatabaseName("idx_user_system_roles_role_id");

        builder.HasOne(x => x.User).WithMany(x => x.SystemRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Role).WithMany(x => x.UserSystemRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.GrantedByUser).WithMany().HasForeignKey(x => x.GrantedBy).OnDelete(DeleteBehavior.SetNull);

        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
    }
}
