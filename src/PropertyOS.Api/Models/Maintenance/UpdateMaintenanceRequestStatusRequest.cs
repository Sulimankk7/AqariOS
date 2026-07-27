using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Api.Models.Maintenance;

/// <summary>
/// Request model for transitioning a maintenance request to a new status.
/// </summary>
public record UpdateMaintenanceRequestStatusRequest(
    MaintenanceStatus NewStatus,
    string? Reason = null
);
