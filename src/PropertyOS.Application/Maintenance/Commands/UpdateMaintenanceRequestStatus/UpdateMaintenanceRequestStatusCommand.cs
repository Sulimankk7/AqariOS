using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Application.Maintenance.Commands.UpdateMaintenanceRequestStatus;

public record UpdateMaintenanceRequestStatusCommand(
    Guid Id,
    MaintenanceStatus NewStatus,
    string? Reason = null
) : ICommand;
