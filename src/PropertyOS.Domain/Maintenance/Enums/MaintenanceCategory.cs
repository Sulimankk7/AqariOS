namespace PropertyOS.Domain.Maintenance.Enums;

/// <summary>
/// Maps to <c>maintenance_category_enum</c> in PostgreSQL.
/// Closed list per spec §8.1 — company cannot define custom categories.
/// </summary>
public enum MaintenanceCategory
{
    Electrical,
    Plumbing,
    AirConditioning,
    Elevator,
    Cleaning,
    Water,
    Structural,
    DoorsWindows,
    Internet,
    Other
}
