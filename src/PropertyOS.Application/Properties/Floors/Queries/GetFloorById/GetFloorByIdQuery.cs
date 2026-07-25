using System;
using MediatR;
using PropertyOS.Application.Properties.Floors.Queries.Common;

namespace PropertyOS.Application.Properties.Floors.Queries.GetFloorById;

public record GetFloorByIdQuery(Guid Id) : IRequest<FloorDto?>;
