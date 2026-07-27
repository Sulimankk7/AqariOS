using System;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Api.Models.Maintenance;

/// <summary>
/// Request model for updating an existing maintenance request.
/// </summary>
public record UpdateMaintenanceRequestRequest(
    Guid BuildingId,
    string Title,
    string Description,
    MaintenanceCategory Category,
    MaintenancePriority Priority,
    Guid? ApartmentId = null,
    Guid? TenantId = null,
    string? InternalNotes = null
);
