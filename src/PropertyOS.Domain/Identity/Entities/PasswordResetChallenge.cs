using System.Net;
using PropertyOS.Domain.Audit.Attributes;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Domain.Identity.Entities;

public sealed class PasswordResetChallenge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public PasswordResetChallengeKind Kind { get; set; }

    [Sensitive]
    public string CredentialHash { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public short FailedAttempts { get; set; }
    public short MaxAttempts { get; set; }
    public IPAddress RequestedIp { get; set; } = IPAddress.Loopback;
    public IPAddress? VerifiedIp { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
