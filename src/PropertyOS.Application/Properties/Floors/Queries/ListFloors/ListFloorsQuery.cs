using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Properties.Floors.Queries.Common;

namespace PropertyOS.Application.Properties.Floors.Queries.ListFloors;

public record ListFloorsQuery(Guid BuildingId) : IRequest<List<FloorDto>>;
