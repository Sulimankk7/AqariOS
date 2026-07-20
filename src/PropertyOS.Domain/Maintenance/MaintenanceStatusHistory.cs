using System;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Domain.Maintenance;

/// <summary>
/// Append-only log of every status transition on a <see cref="MaintenanceRequest"/>.
/// Structural analogue of <c>ContractStatusHistory</c> (Module 5 §5.4).
///
/// IMMUTABILITY: This entity is never soft-deleted or updated — a status transition,
/// once recorded, is a permanent historical fact. Correction is performed by
/// writing a new compensating row with an explanatory <see cref="Reason"/>.
/// </summary>
public class MaintenanceStatusHistory
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid MaintenanceRequestId { get; private set; }

    /// <summary>
    /// NULL only for the very first history row created when the request is opened
    /// (there is no previous state for a brand-new request).
    /// </summary>
    public MaintenanceStatus? PreviousStatus { get; private set; }

    public MaintenanceStatus NewStatus { get; private set; }

    /// <summary>
    /// FK to users; nullable for future automated/SLA-timeout transitions.
    /// </summary>
    public Guid? ChangedBy { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }

    /// <summary>
    /// Optional business justification, e.g. "vendor confirmed repair complete".
    /// </summary>
    public string? Reason { get; private set; }

    private MaintenanceStatusHistory() { }

    public static MaintenanceStatusHistory Create(
        Guid companyId,
        Guid maintenanceRequestId,
        MaintenanceStatus newStatus,
        DateTimeOffset changedAt,
        MaintenanceStatus? previousStatus = null,
        Guid? changedBy = null,
        string? reason = null)
    {
        return new MaintenanceStatusHistory
        {
            Id = Guid.Empty, // DB assigns uuid_generate_v7() on INSERT
            CompanyId = companyId,
            MaintenanceRequestId = maintenanceRequestId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedBy = changedBy,
            ChangedAt = changedAt,
            Reason = reason?.Trim()
        };
    }
}
