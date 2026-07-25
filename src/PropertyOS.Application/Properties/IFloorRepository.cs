using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties;

/// <summary>
/// Repository interface for managing <see cref="Floor"/> entities within buildings.
/// </summary>
public interface IFloorRepository
{
    /// <summary>
    /// Gets a floor by identifier.
    /// </summary>
    Task<Floor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new floor to the change tracker.
    /// </summary>
    Task AddAsync(Floor floor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a floor number already exists within a specified building.
    /// </summary>
    Task<bool> ExistsByFloorNumberAsync(Guid buildingId, short floorNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active floors for a specified building ordered by FloorNumber.
    /// </summary>
    Task<List<Floor>> ListByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
