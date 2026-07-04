using System.Net;
using PropertyOS.Domain.Audit.Enums;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Domain.Audit.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? CompanyId { get; set; }
    public string EntityName { get; set; } = null!;
    public Guid? EntityId { get; set; }
    public AuditAction Action { get; set; }
    public string? PreviousValues { get; set; }
    public string? NewValues { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public IPAddress? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? RequestId { get; set; }
    public Guid? CorrelationId { get; set; }
    public string Metadata { get; set; } = "{}";
    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;
    public AuditSource Source { get; set; } = AuditSource.Api;
    public DateTimeOffset CreatedAt { get; set; }

    public User? ActorUser { get; set; }
    public Company? Company { get; set; }
}
