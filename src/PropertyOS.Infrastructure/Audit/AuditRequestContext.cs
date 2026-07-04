using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Audit.Enums;

namespace PropertyOS.Infrastructure.Audit;

public class AuditRequestContext : IAuditRequestContext
{
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor? _httpContextAccessor;

    public AuditRequestContext(Microsoft.AspNetCore.Http.IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? RequestId
    {
        get
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context == null) return null;
            
            // X-Request-ID is typical, but we check TraceIdentifier as fallback
            if (context.Request.Headers.TryGetValue("X-Request-ID", out var requestIdValue) && 
                Guid.TryParse(requestIdValue.ToString(), out var requestId))
            {
                return requestId;
            }

            if (Guid.TryParse(context.TraceIdentifier, out var traceId))
            {
                return traceId;
            }

            return null;
        }
    }

    public Guid? CorrelationId
    {
        get
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context == null) return null;
            
            if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationIdValue) && 
                Guid.TryParse(correlationIdValue.ToString(), out var correlationId))
            {
                return correlationId;
            }
            
            return null;
        }
    }

    public AuditSource Source => _httpContextAccessor?.HttpContext != null ? AuditSource.Api : AuditSource.SystemJob;
}
