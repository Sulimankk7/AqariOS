using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.DeleteTenantVehicle;

public record DeleteTenantVehicleCommand(
    Guid TenantId,
    Guid VehicleId
) : ICommand;
