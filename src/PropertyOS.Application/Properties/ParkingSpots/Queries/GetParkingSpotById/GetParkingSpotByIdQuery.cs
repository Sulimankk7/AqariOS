using System;
using MediatR;
using PropertyOS.Application.Properties.ParkingSpots.Queries.Common;

namespace PropertyOS.Application.Properties.ParkingSpots.Queries.GetParkingSpotById;

public record GetParkingSpotByIdQuery(Guid Id) : IRequest<ParkingSpotDto?>;
