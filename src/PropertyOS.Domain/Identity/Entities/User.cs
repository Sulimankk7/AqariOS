using System.Net;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Domain.Audit.Attributes;

namespace PropertyOS.Domain.Identity.Entities;

public class User : ISoftDeletable
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    [Sensitive]
    public string? PasswordHash { get; set; }
    public string PasswordAlgorithm { get; set; } = "argon2id";
    public string FullName { get; set; } = null!;
    public string PreferredLanguage { get; set; } = "ar";
    public DateTimeOffset? EmailVerifiedAt { get; set; }
    public DateTimeOffset? PhoneVerifiedAt { get; set; }
    [Sensitive]
    public string? PasswordResetTokenHash { get; set; }
    public DateTimeOffset? PasswordResetExpiresAt { get; set; }
    public short FailedLoginAttempts { get; set; } = 0;
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public IPAddress? LastLoginIp { get; set; }
    public bool MfaEnabled { get; set; } = false;
    [Sensitive]
    public string? MfaSecretEncrypted { get; set; }
    public MfaType? MfaType { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    
    public User? DeletedByUser { get; set; }
    public ICollection<UserCompanyRole> CompanyRoles { get; set; } = new List<UserCompanyRole>();
    public ICollection<UserSystemRole> SystemRoles { get; set; } = new List<UserSystemRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();
}
