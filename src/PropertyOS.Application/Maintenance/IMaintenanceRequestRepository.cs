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
    /// Verifies that a file exists in the file storage subsystem before it may be attached
    /// to a maintenance request.
    ///
    /// <para>
    /// <b>DEFERRED — do not call from any Module 8 handler.</b>
    /// The File storage module (Module 10) has not yet been implemented.
    /// This method exists as a forward-compatible abstraction so that enabling file validation
    /// later requires only: (1) a real implementation, and (2) a call-site in
    /// <see cref="Commands.AddMaintenanceAttachment.AddMaintenanceAttachmentCommandHandler"/>.
    /// Until then, <c>file_id</c> is stored as a raw nullable UUID with no FK constraint,
    /// and attachment creation succeeds without file verification.
    /// </para>
    /// </summary>
    Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default);
}
