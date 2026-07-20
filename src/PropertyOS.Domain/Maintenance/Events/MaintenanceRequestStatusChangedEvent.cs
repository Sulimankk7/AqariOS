using System;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Domain.Maintenance.Events;

/// <summary>
/// Raised by <see cref="MaintenanceRequest.UpdateStatus"/> each time a request
/// moves to a new status. Consumers may use this for notifications, SLA tracking,
/// or audit enrichment.
/// </summary>
public sealed record MaintenanceRequestStatusChangedEvent(
    Guid RequestId,
    Guid CompanyId,
    MaintenanceStatus? OldStatus,
    MaintenanceStatus NewStatus,
    DateTimeOffset ChangedAt);
