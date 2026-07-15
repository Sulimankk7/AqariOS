using System;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Leasing;

public interface ILeasingReferenceRepository
{
    Task<Guid?> GetApartmentBuildingIdAsync(Guid apartmentId, Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> TenantExistsAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default);
}
