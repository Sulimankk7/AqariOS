using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Application.Maintenance.Commands.UpdateMaintenanceRequest;

public record UpdateMaintenanceRequestCommand(
    Guid Id,
    Guid BuildingId,
    Guid? ApartmentId,
    Guid? TenantId,
    string Title,
    string Description,
    MaintenanceCategory Category,
    MaintenancePriority Priority,
    string? InternalNotes
) : ICommand;
