using MediatR;
using PropertyOS.Application.Properties.ParkingAssignments.Queries.Common;

namespace PropertyOS.Application.Properties.ParkingAssignments.Queries.GetAssignedParkingByLease;

public record GetAssignedParkingByLeaseQuery(Guid LeaseContractId) : IRequest<List<ParkingAssignmentDto>>;
