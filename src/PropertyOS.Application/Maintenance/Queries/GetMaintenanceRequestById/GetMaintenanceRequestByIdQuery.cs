using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Maintenance.Queries.Common;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceRequestById;

public record GetMaintenanceRequestByIdQuery(Guid Id) : IRequest<MaintenanceRequestDetailDto?>;
