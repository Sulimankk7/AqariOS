using PropertyOS.Application.Properties.ParkingAssignments.Queries.Common;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties;

public interface IParkingAssignmentRepository
{
    Task<ParkingAssignment?> GetByIdForUpdateAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveAssignmentAsync(Guid parkingSpotId, Guid companyId, CancellationToken cancellationToken = default);
    Task<ParkingAssignmentDto?> GetCurrentBySpotAsync(Guid parkingSpotId, Guid companyId, CancellationToken cancellationToken = default);
    Task<List<ParkingAssignmentDto>> GetCurrentByLeaseAsync(Guid leaseContractId, Guid companyId, CancellationToken cancellationToken = default);
    Task EndActiveByLeaseAsync(Guid leaseContractId, Guid companyId, DateOnly endedOn,
        DateTimeOffset updatedAt, Guid? updatedBy, CancellationToken cancellationToken = default);
    Task AddAsync(ParkingAssignment assignment, CancellationToken cancellationToken = default);
}
