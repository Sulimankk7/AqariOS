using System.Net;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Domain.Identity.Entities;

public class LoginHistory
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? AttemptedIdentifier { get; set; }
    public Guid? CompanyId { get; set; }
    public LoginStatus Status { get; set; }
    public IPAddress IpAddress { get; set; } = null!;
    public string? UserAgent { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? GeolocationCountry { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User? User { get; set; }
    public Company? Company { get; set; }
}
