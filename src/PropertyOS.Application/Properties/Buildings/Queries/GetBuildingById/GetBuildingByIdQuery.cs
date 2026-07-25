using System;
using MediatR;
using PropertyOS.Application.Properties.Buildings.Queries.Common;

namespace PropertyOS.Application.Properties.Buildings.Queries.GetBuildingById;

public record GetBuildingByIdQuery(Guid Id) : IRequest<BuildingDto?>;
