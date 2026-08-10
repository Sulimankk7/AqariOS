using System;
using MediatR;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetVehicleById;

public record GetVehicleByIdQuery(
    Guid TenantId,
    Guid VehicleId
) : IRequest<TenantVehicleDto?>;
