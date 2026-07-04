using System.Net;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Audit.Attributes;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Domain.Identity.Entities;

public class OtpChallenge
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = null!;
    public OtpPurpose Purpose { get; set; }
    [Sensitive]
    public string CodeHash { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public short FailedAttempts { get; set; } = 0;
    public short MaxAttempts { get; set; }
    public IPAddress RequestedIp { get; set; } = null!;
    public IPAddress? VerifiedIp { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
