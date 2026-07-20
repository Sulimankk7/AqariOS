namespace PropertyOS.Domain.Maintenance.Enums;

/// <summary>
/// Maps to <c>maintenance_priority_enum</c> in PostgreSQL.
/// Ordered by severity: Low → Medium → High → Emergency.
/// </summary>
public enum MaintenancePriority
{
    Low,
    Medium,
    High,
    Emergency
}
