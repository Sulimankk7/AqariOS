namespace PropertyOS.Domain.Properties.Enums;

/// <summary>
/// Apartment occupancy state. Maps to PostgreSQL enum <c>occupancy_status_enum</c>.
/// Approved values per Module 4 §4.4.
///
/// <b>DEFERRED OWNERSHIP (Module 5):</b>
/// This column is a denormalized cache — the authoritative source of truth is an
/// active <c>lease_contracts</c> row. The trigger that refreshes this column when
/// a lease is created, activated, or terminated is defined in Module 5. In Phase 1
/// the column is created with a default of <c>vacant</c> and is correct by
/// definition until Module 5 lease logic is wired in.
/// </summary>
public enum OccupancyStatus
{
    Vacant,
    Occupied,
    UnderMaintenance,
    Listed,
}
