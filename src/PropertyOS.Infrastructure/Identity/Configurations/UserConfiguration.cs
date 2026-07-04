using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Infrastructure.Identity.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", t =>
        {
            t.HasCheckConstraint("chk_users_email_or_phone", "email IS NOT NULL OR phone IS NOT NULL");
            t.HasCheckConstraint("chk_users_mfa_secret_required", "(mfa_enabled = false) OR (mfa_secret_encrypted IS NOT NULL AND mfa_type IS NOT NULL)");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").HasDefaultValueSql("uuid_generate_v7()").ValueGeneratedOnAdd();
        
        builder.Property(e => e.Email).HasColumnName("email").HasColumnType("citext").IsRequired(false);
        builder.Property(e => e.Phone).HasColumnName("phone").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired(false);
        builder.Property(e => e.PasswordHash).HasColumnName("password_hash").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.PasswordAlgorithm).HasColumnName("password_algorithm").HasColumnType("character varying(20)").HasMaxLength(20).HasDefaultValue("argon2id").IsRequired();
        builder.Property(e => e.FullName).HasColumnName("full_name").HasColumnType("character varying(255)").HasMaxLength(255).IsRequired();
        builder.Property(e => e.PreferredLanguage).HasColumnName("preferred_language").HasColumnType("character(2)").HasMaxLength(2).HasDefaultValue("ar").IsRequired();
        builder.Property(e => e.EmailVerifiedAt).HasColumnName("email_verified_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.PhoneVerifiedAt).HasColumnName("phone_verified_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.PasswordResetTokenHash).HasColumnName("password_reset_token_hash").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.PasswordResetExpiresAt).HasColumnName("password_reset_expires_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.FailedLoginAttempts).HasColumnName("failed_login_attempts").HasColumnType("smallint").HasDefaultValue(0).IsRequired();
        builder.Property(e => e.LockedUntil).HasColumnName("locked_until").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.LastLoginAt).HasColumnName("last_login_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.LastLoginIp).HasColumnName("last_login_ip").HasColumnType("inet").IsRequired(false);
        builder.Property(e => e.MfaEnabled).HasColumnName("mfa_enabled").HasColumnType("boolean").HasDefaultValue(false).IsRequired();
        builder.Property(e => e.MfaSecretEncrypted).HasColumnName("mfa_secret_encrypted").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.MfaType).HasColumnName("mfa_type").HasColumnType("mfa_type_enum").IsRequired(false);
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasColumnType("boolean").HasDefaultValue(true).IsRequired();
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasColumnType("uuid").IsRequired(false);

        builder.HasIndex(e => e.Email).HasDatabaseName("uq_users_email").IsUnique(); 
        builder.HasIndex(e => e.Phone).HasDatabaseName("uq_users_phone").IsUnique(); 
        builder.HasIndex(e => e.LockedUntil).HasDatabaseName("idx_users_locked_until"); 

        builder.HasOne(e => e.DeletedByUser)
            .WithMany()
            .HasForeignKey(e => e.DeletedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(e => e.DeletedAt == null);

        builder.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
    }
}
