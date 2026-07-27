using System;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Api.Models.Maintenance;

/// <summary>
/// Request model for creating a new maintenance request.
/// </summary>
public record CreateMaintenanceRequestRequest(
    Guid BuildingId,
    string Title,
    string Description,
    MaintenanceCategory Category,
    MaintenancePriority Priority,
    DateOnly RequestDate,
    Guid? ApartmentId = null,
    Guid? TenantId = null
);
