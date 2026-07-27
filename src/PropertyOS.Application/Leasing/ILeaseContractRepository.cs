using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Application.Leasing;

public interface ILeaseContractRepository
{
    Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default);
    Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default);
    Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default);
    Task<bool> HasSuccessorContractAsync(Guid priorContractId, CancellationToken cancellationToken = default);
    Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default);
    Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default);
    /// <summary>
    /// Enumerates the IDs of Active contracts whose EndDate &lt;= asOfDate. Keyset cursor
    /// over Id — returns up to batchSize IDs strictly greater than <paramref name="afterId"/>
    /// (all rows when null), ordered by Id. Poison-id skipping is the job's responsibility.
    /// </summary>
    Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates the IDs of all Active contracts (no date filter). Keyset cursor over Id —
    /// returns up to batchSize IDs strictly greater than <paramref name="afterId"/> (all rows
    /// when null), ordered by Id. Used by the scheduled-installment generation sweep job.
    /// </summary>
    Task<List<Guid>> GetActiveContractIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken = default);

    // Read-side projections. Explicitly tenant-scoped: plain queries run outside a
    // transaction, so RLS tenant context is not guaranteed — the companyId predicate
    // is the enforced boundary here (Architecture §7).
    Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default);
    Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default);
    Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default);
    Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default);
}
