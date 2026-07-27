using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Application.Maintenance.Commands.CreateMaintenanceRequest;

public record CreateMaintenanceRequestCommand(
    Guid BuildingId,
    Guid? ApartmentId,
    Guid? TenantId,
    string Title,
    string Description,
    MaintenanceCategory Category,
    MaintenancePriority Priority,
    DateOnly RequestDate
) : ICommand<Guid>;
