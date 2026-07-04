using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Infrastructure.Identity.Configurations;

internal sealed class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable("otp_challenges", t =>
        {
            t.HasCheckConstraint("chk_otp_challenges_expiry", "expires_at > created_at");
            t.HasCheckConstraint("chk_otp_challenges_failed_attempts", "failed_attempts >= 0 AND max_attempts > 0 AND failed_attempts <= max_attempts");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        
        builder.Property(e => e.Phone).HasColumnName("phone").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Purpose).HasColumnName("purpose").HasColumnType("otp_purpose_enum").IsRequired();
        builder.Property(e => e.CodeHash).HasColumnName("code_hash").HasColumnType("text").IsRequired();
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.ConsumedAt).HasColumnName("consumed_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.FailedAttempts).HasColumnName("failed_attempts").HasColumnType("smallint").HasDefaultValue(0).IsRequired();
        builder.Property(e => e.MaxAttempts).HasColumnName("max_attempts").HasColumnType("smallint").IsRequired();
        builder.Property(e => e.RequestedIp).HasColumnName("requested_ip").HasColumnType("inet").IsRequired();
        builder.Property(e => e.VerifiedIp).HasColumnName("verified_ip").HasColumnType("inet").IsRequired(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(e => new { e.Phone, e.Purpose, e.CreatedAt }).HasDatabaseName("idx_otp_challenges_phone_purpose_created_at");
        builder.HasIndex(e => e.ExpiresAt).HasDatabaseName("idx_otp_challenges_expires_at"); 
    }
}
