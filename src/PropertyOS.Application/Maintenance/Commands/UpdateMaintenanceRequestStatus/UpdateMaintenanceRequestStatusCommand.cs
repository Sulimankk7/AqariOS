using System;
using MediatR;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Application.Maintenance.Commands.UpdateMaintenanceRequestStatus;

public record UpdateMaintenanceRequestStatusCommand(
    Guid Id,
    MaintenanceStatus NewStatus,
    string? Reason = null
) : IRequest<MediatR.Unit>;
