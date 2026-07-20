using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Maintenance;

namespace PropertyOS.Application.Maintenance;

/// <summary>
/// Write-side repository for the <see cref="MaintenanceRequest"/> aggregate.
/// Responsible exclusively for aggregate persistence — no read projections.
/// Read-side projections live in <see cref="IMaintenanceQueries"/>.
///
/// <para>CQRS separation: this interface is injected only into command handlers.</para>
/// </summary>
public interface IMaintenanceRequestRepository
{
    /// <summary>
    /// Loads a request with its active attachments and comments included,
    /// so the aggregate can enforce child-collection invariants.
    /// Returns null if not found or if the request belongs to a different tenant (RLS-enforced).
    /// </summary>
    Task<Domain.Maintenance.MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new request to the change-tracked context.</summary>
    Task AddAsync(Domain.Maintenance.MaintenanceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a <see cref="MaintenanceStatusHistory"/> row.
    /// Must be called in the same Unit-of-Work as the parent request update.
    /// </summary>
    Task AddStatusHistoryAsync(MaintenanceStatusHistory history, CancellationToken cancellationToken = default);

    // -------------------------------------------------------------------------
    // Reference existence checks (guards in command handlers)
    // -------------------------------------------------------------------------

    Task<bool> BuildingExistsAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> ApartmentExistsAsync(Guid apartmentId, Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> TenantExistsAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that a file exists in <c>file_storage</c> before allowing it
    /// to be attached. Prevents attaching phantom file references.
    /// </summary>
    Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default);
}
