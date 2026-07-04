using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Infrastructure.Identity.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", t =>
        {
            t.HasCheckConstraint("chk_refresh_tokens_revoked_reason", "revoked_reason IS NOT NULL OR revoked_at IS NULL");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        
        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.TokenHash).HasColumnName("token_hash").HasColumnType("text").IsRequired();
        builder.Property(e => e.FamilyId).HasColumnName("family_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.ReplacedByTokenId).HasColumnName("replaced_by_token_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.DeviceFingerprint).HasColumnName("device_fingerprint").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.DeviceName).HasColumnName("device_name").HasColumnType("character varying(255)").HasMaxLength(255).IsRequired(false);
        builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasColumnType("inet").IsRequired();
        builder.Property(e => e.UserAgent).HasColumnName("user_agent").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.IssuedAt).HasColumnName("issued_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.RevokedReason).HasColumnName("revoked_reason").HasColumnType("revoke_reason_enum").IsRequired(false);

        builder.HasIndex(e => e.TokenHash).HasDatabaseName("uq_refresh_tokens_token_hash").IsUnique();
        builder.HasIndex(e => e.UserId).HasDatabaseName("idx_refresh_tokens_user_id"); 
        builder.HasIndex(e => e.FamilyId).HasDatabaseName("idx_refresh_tokens_family_id");
        builder.HasIndex(e => e.ExpiresAt).HasDatabaseName("idx_refresh_tokens_expires_at"); 

        builder.HasOne(e => e.User).WithMany(u => u.RefreshTokens).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.ReplacedByToken).WithMany().HasForeignKey(e => e.ReplacedByTokenId).OnDelete(DeleteBehavior.SetNull);
    }
}
