using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Infrastructure.Identity.Configurations;

internal sealed class PasswordResetChallengeConfiguration : IEntityTypeConfiguration<PasswordResetChallenge>
{
    public void Configure(EntityTypeBuilder<PasswordResetChallenge> builder)
    {
        builder.ToTable("password_reset_challenges", table =>
        {
            table.HasCheckConstraint("chk_password_reset_challenges_expiry", "expires_at > created_at");
            table.HasCheckConstraint(
                "chk_password_reset_challenges_attempts",
                "failed_attempts >= 0 AND max_attempts > 0 AND failed_attempts <= max_attempts");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        builder.Property(e => e.Kind).HasColumnName("kind").HasColumnType("character varying(32)")
            .HasMaxLength(32).HasConversion<string>().IsRequired();
        builder.Property(e => e.CredentialHash).HasColumnName("credential_hash").HasColumnType("text").IsRequired();
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(e => e.ConsumedAt).HasColumnName("consumed_at").HasColumnType("timestamp with time zone");
        builder.Property(e => e.FailedAttempts).HasColumnName("failed_attempts").HasColumnType("smallint").HasDefaultValue((short)0).IsRequired();
        builder.Property(e => e.MaxAttempts).HasColumnName("max_attempts").HasColumnType("smallint").IsRequired();
        builder.Property(e => e.RequestedIp).HasColumnName("requested_ip").HasColumnType("inet").IsRequired();
        builder.Property(e => e.VerifiedIp).HasColumnName("verified_ip").HasColumnType("inet");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()").IsRequired();

        builder.HasIndex(e => e.CredentialHash).HasDatabaseName("uq_password_reset_challenges_credential_hash").IsUnique();
        builder.HasIndex(e => new { e.UserId, e.Kind, e.CreatedAt })
            .HasDatabaseName("idx_password_reset_challenges_user_kind_created_at");
        builder.HasIndex(e => e.ExpiresAt).HasDatabaseName("idx_password_reset_challenges_expires_at");

        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
