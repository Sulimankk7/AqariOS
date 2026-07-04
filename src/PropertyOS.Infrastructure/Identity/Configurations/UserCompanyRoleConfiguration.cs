using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Infrastructure.Identity.Configurations;

internal sealed class UserCompanyRoleConfiguration : IEntityTypeConfiguration<UserCompanyRole>
{
    public void Configure(EntityTypeBuilder<UserCompanyRole> builder)
    {
        builder.ToTable("user_company_roles", t =>
        {
            t.HasCheckConstraint("chk_user_company_roles_joined_at", "joined_at IS NULL OR joined_at >= invited_at");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        
        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.CompanyId).HasColumnName("company_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.RoleId).HasColumnName("role_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasColumnType("membership_status_enum").HasDefaultValue(MembershipStatus.InvitedPending).IsRequired();
        builder.Property(e => e.InvitedAt).HasColumnName("invited_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.JoinedAt).HasColumnName("joined_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.SuspendedAt).HasColumnName("suspended_at").HasColumnType("timestamp with time zone").IsRequired(false);
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasColumnType("uuid").IsRequired(false);

        builder.HasIndex(e => new { e.UserId, e.CompanyId }).HasDatabaseName("uq_user_company_roles_user_company").IsUnique(); 
        builder.HasIndex(e => e.UserId).HasDatabaseName("idx_user_company_roles_user_id"); 
        builder.HasIndex(e => new { e.CompanyId, e.Status }).HasDatabaseName("idx_user_company_roles_company_status"); 

        builder.HasOne(e => e.User).WithMany(u => u.CompanyRoles).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Role).WithMany(r => r.UserCompanyRoles).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => e.DeletedAt == null);

        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
    }
}
