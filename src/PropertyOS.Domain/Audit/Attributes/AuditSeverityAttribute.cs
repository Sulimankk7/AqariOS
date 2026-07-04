using System;
using PropertyOS.Domain.Audit.Enums;

namespace PropertyOS.Domain.Audit.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class AuditSeverityAttribute : Attribute
{
    public AuditSeverity Severity { get; }

    public AuditSeverityAttribute(AuditSeverity severity)
    {
        Severity = severity;
    }
}
