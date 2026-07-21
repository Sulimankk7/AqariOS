using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties;

public interface IApartmentRepository
{
    Task<Apartment?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> BuildingExistsAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default);
}
