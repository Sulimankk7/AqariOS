using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Infrastructure.Identity.Configurations;

internal sealed class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> builder)
    {
        builder.ToTable("login_history", t =>
        {
            t.HasCheckConstraint("chk_login_history_success_has_user", "status != 'success' OR user_id IS NOT NULL");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        
        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.AttemptedIdentifier).HasColumnName("attempted_identifier").HasColumnType("character varying(255)").HasMaxLength(255).IsRequired(false);
        builder.Property(e => e.CompanyId).HasColumnName("company_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.Status).HasColumnName("status").HasColumnType("login_status_enum").IsRequired();
        builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasColumnType("inet").IsRequired();
        builder.Property(e => e.UserAgent).HasColumnName("user_agent").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.DeviceFingerprint).HasColumnName("device_fingerprint").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.GeolocationCountry).HasColumnName("geolocation_country").HasColumnType("character(2)").HasMaxLength(2).IsRequired(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(e => new { e.UserId, e.CreatedAt }).HasDatabaseName("idx_login_history_user_id_created_at"); 
        builder.HasIndex(e => new { e.IpAddress, e.CreatedAt }).HasDatabaseName("idx_login_history_ip_created_at");
        builder.HasIndex(e => new { e.Status, e.CreatedAt }).HasDatabaseName("idx_login_history_status_created_at"); 

        builder.HasOne(e => e.User).WithMany(u => u.LoginHistories).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.SetNull);
    }
}
