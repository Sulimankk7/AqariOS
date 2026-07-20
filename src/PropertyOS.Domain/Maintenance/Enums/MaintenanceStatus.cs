namespace PropertyOS.Domain.Maintenance.Enums;

/// <summary>
/// Maps to <c>maintenance_status_enum</c> in PostgreSQL.
/// Terminal states: <see cref="Closed"/> and <see cref="Cancelled"/>.
/// See <see cref="PropertyOS.Domain.Maintenance.MaintenanceRequest"/> for the valid transition graph.
/// </summary>
public enum MaintenanceStatus
{
    Open,
    InProgress,
    Waiting,
    Resolved,
    Closed,
    Cancelled
}
