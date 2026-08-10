using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.UpdateTenantVehicle;

public record UpdateTenantVehicleCommand(
    Guid TenantId,
    Guid VehicleId,
    string PlateNumber,
    string MakeModel,
    string Color
) : ICommand;
