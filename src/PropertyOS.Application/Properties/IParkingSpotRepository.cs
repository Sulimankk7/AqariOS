using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties;

/// <summary>
/// Repository interface for managing <see cref="ParkingSpot"/> entities within buildings.
/// </summary>
public interface IParkingSpotRepository
{
    /// <summary>
    /// Gets a parking spot by identifier.
    /// </summary>
    Task<ParkingSpot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new parking spot to the change tracker.
    /// </summary>
    Task AddAsync(ParkingSpot spot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a spot code already exists within a specified building.
    /// </summary>
    Task<bool> ExistsBySpotCodeAsync(Guid buildingId, string spotCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active parking spots belonging to a specified building.
    /// </summary>
    Task<List<ParkingSpot>> ListByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
