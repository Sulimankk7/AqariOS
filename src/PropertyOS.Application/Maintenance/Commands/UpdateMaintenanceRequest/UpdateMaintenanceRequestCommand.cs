using System;
using MediatR;
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
) : IRequest<MediatR.Unit>;
