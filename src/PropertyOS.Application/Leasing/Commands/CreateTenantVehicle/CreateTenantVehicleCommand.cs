using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.CreateTenantVehicle;

public record CreateTenantVehicleCommand(
    Guid TenantId,
    string PlateNumber,
    string MakeModel,
    string Color
) : ICommand<Guid>;
