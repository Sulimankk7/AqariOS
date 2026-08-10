using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Application.Leasing.Queries.GetVehiclesForTenant;

public record GetVehiclesForTenantQuery(
    Guid TenantId
) : IRequest<List<TenantVehicleDto>>;
