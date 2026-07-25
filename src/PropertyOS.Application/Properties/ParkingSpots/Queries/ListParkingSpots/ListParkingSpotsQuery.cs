using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Properties.ParkingSpots.Queries.Common;

namespace PropertyOS.Application.Properties.ParkingSpots.Queries.ListParkingSpots;

public record ListParkingSpotsQuery(Guid BuildingId) : IRequest<List<ParkingSpotDto>>;
