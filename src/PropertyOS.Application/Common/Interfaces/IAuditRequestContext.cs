using PropertyOS.Domain.Audit.Enums;

namespace PropertyOS.Application.Common.Interfaces;

public interface IAuditRequestContext
{
    Guid? RequestId { get; }
    Guid? CorrelationId { get; }
    AuditSource Source { get; }
}
