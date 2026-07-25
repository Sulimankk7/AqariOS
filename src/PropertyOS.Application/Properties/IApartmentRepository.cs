using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties;

/// <summary>
/// Repository interface for managing <see cref="Apartment"/> (rentable unit) entities.
/// </summary>
public interface IApartmentRepository
{
    /// <summary>
    /// Gets an apartment by identifier and explicit company scope.
    /// Preserved for backward compatibility with Module 5 / Module 9 integration.
    /// </summary>
    Task<Apartment?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an apartment by identifier using PostgreSQL session RLS context.
    /// </summary>
    Task<Apartment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an apartment with its full parent hierarchy context.
    /// </summary>
    Task<Apartment?> GetWithHierarchyByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new apartment to the change tracker.
    /// </summary>
    Task AddAsync(Apartment apartment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a building exists for the given company.
    /// Preserved for backward compatibility.
    /// </summary>
    Task<bool> BuildingExistsAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a unit number already exists within a specified building.
    /// </summary>
    Task<bool> UnitNumberExistsInBuildingAsync(Guid buildingId, string unitNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active apartments belonging to a specified building.
    /// </summary>
    Task<List<Apartment>> ListByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active apartments belonging to a specified floor.
    /// </summary>
    Task<List<Apartment>> ListByFloorIdAsync(Guid floorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active apartments belonging to a specified company.
    /// </summary>
    Task<List<Apartment>> ListByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
