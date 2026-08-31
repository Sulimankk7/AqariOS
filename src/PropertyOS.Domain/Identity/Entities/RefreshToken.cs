using System.Net;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Audit.Attributes;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Domain.Identity.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    [Sensitive]
    public string TokenHash { get; set; } = null!;
    public Guid FamilyId { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? DeviceName { get; set; }
    public IPAddress IpAddress { get; set; } = null!;
    public string? UserAgent { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsPersistent { get; set; } = true;
    public DateTimeOffset? AbsoluteSessionExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public RevokeReason? RevokedReason { get; set; }

    public User User { get; set; } = null!;
    public RefreshToken? ReplacedByToken { get; set; }
}
