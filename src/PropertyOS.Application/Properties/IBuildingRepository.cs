using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties;

/// <summary>
/// Repository interface for managing <see cref="Building"/> aggregates and their 1:1 <see cref="BuildingAddress"/> extensions.
/// </summary>
public interface IBuildingRepository
{
    /// <summary>
    /// Gets a building by identifier, including its address extension.
    /// </summary>
    Task<Building?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new building (and address extension) to the change tracker.
    /// </summary>
    Task AddAsync(Building building, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a building with the specified name exists for the given company.
    /// </summary>
    Task<bool> ExistsByNameAsync(Guid companyId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a building with the specified internal reference code exists for the given company.
    /// </summary>
    Task<bool> ExistsByCodeAsync(Guid companyId, string internalCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active buildings for a specified company.
    /// </summary>
    Task<List<Building>> ListByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active buildings for a specified company (for plan subscription limit enforcement).
    /// </summary>
    Task<int> GetCountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
