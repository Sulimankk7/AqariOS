using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Properties.ParkingAssignments.Queries.Common;

namespace PropertyOS.Application.Properties.ParkingAssignments.Queries.GetCurrentAssignmentBySpot;

public record GetCurrentAssignmentBySpotQuery(Guid ParkingSpotId) : IRequest<ParkingAssignmentDto?>;
