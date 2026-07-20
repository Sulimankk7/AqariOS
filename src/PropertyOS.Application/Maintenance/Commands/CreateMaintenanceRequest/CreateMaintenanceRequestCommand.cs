using System;
using MediatR;
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
) : IRequest<Guid>;
