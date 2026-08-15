using Microsoft.EntityFrameworkCore;
using Mapster;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Infrastructure.Leasing.Repositories;

public class LeaseContractRepository : ILeaseContractRepository
{
    private readonly PropertyOS.Infrastructure.Persistence.PropertyOsDbContext _dbContext;

    public LeaseContractRepository(PropertyOS.Infrastructure.Persistence.PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.LeaseContracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.LeaseContracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default)
    {
        await _dbContext.LeaseContracts.AddAsync(leaseContract, cancellationToken);
    }

    public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.LeaseContracts.AnyAsync(
            c => c.ApartmentId == apartmentId && c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Active,
            cancellationToken);
    }

    public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default)
    {
        return _dbContext.LeaseContracts.AnyAsync(
            c => c.ApartmentId == apartmentId
                 && c.Id != excludeContractId
                 && c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Active,
            cancellationToken);
    }

    public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var start = DateOnly.FromDateTime(startDate);
        var end = DateOnly.FromDateTime(endDate);
        return _dbContext.LeaseContracts.AnyAsync(
            c => c.ApartmentId == apartmentId
                 && (c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Draft ||
                     c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.PendingSignature ||
                     c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Active)
                 && c.StartDate < end && c.EndDate > start,
            cancellationToken);
    }

    public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default)
    {
        var start = DateOnly.FromDateTime(startDate);
        var end = DateOnly.FromDateTime(endDate);
        return _dbContext.LeaseContracts.AnyAsync(
            c => c.ApartmentId == apartmentId
                 && c.Id != excludeContractId
                 && (c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Draft ||
                     c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.PendingSignature ||
                     c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Active)
                 && c.StartDate < end && c.EndDate > start,
            cancellationToken);
    }

    public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ContractDocuments.AnyAsync(
            d => d.LeaseContractId == leaseContractId
                 && d.DocumentType == PropertyOS.Domain.Leasing.Enums.ContractDocumentType.SignedContract,
            cancellationToken);
    }

    public Task<bool> HasDocumentAsync(Guid leaseContractId, Guid fileId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ContractDocuments.AnyAsync(
            d => d.LeaseContractId == leaseContractId
                 && d.FileId == fileId,
            cancellationToken);
    }

    public Task<bool> HasSuccessorContractAsync(Guid priorContractId, CancellationToken cancellationToken = default)
    {
        return _dbContext.LeaseContracts.AnyAsync(
            c => c.PriorContractId == priorContractId,
            cancellationToken);
    }

    public async Task AddDocumentAsync(ContractDocument document, CancellationToken cancellationToken = default)
    {
        await _dbContext.ContractDocuments.AddAsync(document, cancellationToken);
    }

    public async Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default)
    {
        await _dbContext.ContractStatusHistory.AddAsync(statusHistory, cancellationToken);
    }

    public async Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default)
    {
        await _dbContext.ContractTerminations.AddAsync(termination, cancellationToken);
    }

    public Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default)
    {
        // Keyset cursor over Id: the job advances afterId with the last returned Id per
        // batch and skips poison ids client-side.
        return _dbContext.LeaseContracts
            .AsNoTracking()
            .Where(c => c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Active
                        && c.EndDate <= asOfDate
                        && (afterId == null || c.Id.CompareTo(afterId.Value) > 0))
            .OrderBy(c => c.Id)
            .Take(batchSize)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Guid>> GetActiveContractIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken = default)
    {
        // Keyset cursor over Id: the job advances afterId with the last returned Id per
        // batch and skips poison ids client-side.
        return _dbContext.LeaseContracts
            .AsNoTracking()
            .Where(c => c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Active
                        && (afterId == null || c.Id.CompareTo(afterId.Value) > 0))
            .OrderBy(c => c.Id)
            .Take(batchSize)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        // Tenant scoping on the aggregate root: cross-tenant ids fall through to null (404),
        // and the child queries below only run for a contract proven to belong to companyId.
        var contractDto = await _dbContext.LeaseContracts
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .ProjectToType<LeaseContractDetailDto>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (contractDto == null)
            return null;

        contractDto.StatusHistory = await _dbContext.ContractStatusHistory
            .AsNoTracking()
            .Where(h => h.LeaseContractId == id)
            .OrderByDescending(h => h.ChangedAt)
            .ProjectToType<ContractStatusHistoryDto>()
            .ToListAsync(cancellationToken);

        contractDto.Documents = await (from d in _dbContext.ContractDocuments.AsNoTracking()
                                       join f in _dbContext.FileStorage.AsNoTracking() on d.FileId equals f.Id into fj
                                       from f in fj.DefaultIfEmpty()
                                       where d.LeaseContractId == id
                                       orderby d.CreatedAt descending
                                       select new ContractDocumentDto
                                       {
                                           Id = d.Id,
                                           CompanyId = d.CompanyId,
                                           LeaseContractId = d.LeaseContractId,
                                           FileId = d.FileId,
                                           DocumentType = d.DocumentType,
                                           Description = d.Description,
                                           OriginalFilename = f != null ? f.OriginalFilename : null,
                                           MimeType = f != null ? f.MimeType : null,
                                           SizeBytes = f != null ? f.SizeBytes : 0,
                                           UploadedBy = d.UploadedBy,
                                           CreatedAt = d.CreatedAt
                                       }).ToListAsync(cancellationToken);


        contractDto.Termination = await _dbContext.ContractTerminations
            .AsNoTracking()
            .Where(t => t.LeaseContractId == id)
            .ProjectToType<ContractTerminationDto>()
            .FirstOrDefaultAsync(cancellationToken);

        return contractDto;
    }

    public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default)
    {
        return _dbContext.LeaseContracts
            .AsNoTracking()
            .Where(c => c.ApartmentId == apartmentId && c.CompanyId == companyId)
            .OrderByDescending(c => c.StartDate)
            .ThenBy(c => c.Id)
            .Take(Math.Clamp(pageSize, 1, 200))
            .ProjectToType<LeaseContractDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default)
    {
        return _dbContext.LeaseContracts
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.CompanyId == companyId)
            .OrderByDescending(c => c.StartDate)
            .ThenBy(c => c.Id)
            .Take(Math.Clamp(pageSize, 1, 200))
            .ProjectToType<LeaseContractDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Clamp(pageSize, 1, 200);

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return _dbContext.LeaseContracts
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId)
                .OrderByDescending(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .Take(effectivePageSize)
                .ProjectToType<LeaseContractDto>()
                .ToListAsync(cancellationToken);
        }

        var normalizedSearch = searchTerm.Trim().ToLower();

        return _dbContext.LeaseContracts
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId &&
                        (EF.Functions.ILike(c.ContractNumber, $"%{normalizedSearch}%") ||
                         (c.ExternalRegistrationRef != null && EF.Functions.ILike(c.ExternalRegistrationRef, $"%{normalizedSearch}%"))))
            .OrderByDescending(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Take(effectivePageSize)
            .ProjectToType<LeaseContractDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var limitDate = today.AddDays(daysAhead);

        return _dbContext.LeaseContracts
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId
                        && c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Active
                        && c.EndDate >= today
                        && c.EndDate <= limitDate)
            .OrderBy(c => c.EndDate)
            .ProjectToType<LeaseContractDto>()
            .ToListAsync(cancellationToken);
    }

    public async Task<ContractDocument?> GetDocumentByIdAsync(Guid leaseContractId, Guid documentId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ContractDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.LeaseContractId == leaseContractId && d.CompanyId == companyId, cancellationToken);
    }

    public async Task<PropertyOS.Application.Leasing.Queries.GetMyActiveLease.TenantLeaseDto?> GetActiveLeaseByTenantIdAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var activeLeases = await (from c in _dbContext.LeaseContracts.AsNoTracking()
                                  join a in _dbContext.Apartments.AsNoTracking() on c.ApartmentId equals a.Id
                                  join b in _dbContext.Buildings.AsNoTracking() on c.BuildingId equals b.Id
                                  where c.CompanyId == companyId
                                     && c.TenantId == tenantId
                                     && c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Active
                                  select new PropertyOS.Application.Leasing.Queries.GetMyActiveLease.TenantLeaseDto(
                                      c.Id,
                                      c.ContractNumber,
                                      c.StartDate,
                                      c.EndDate,
                                      c.SignedDate,
                                      c.MonthlyRentAmount,
                                      c.Currency,
                                      c.SecurityDepositAmount,
                                      c.PaymentFrequency.ToString(),
                                      c.PaymentDueDay,
                                      c.Status.ToString(),
                                      c.LegalRegime.ToString(),
                                      c.TenantType.ToString(),
                                      a.Id,
                                      a.UnitNumber,
                                      a.Bedrooms,
                                      a.Bathrooms,
                                      a.AreaSqm,
                                      b.Id,
                                      b.Name
                                  ))
                                  .ToListAsync(cancellationToken);

        if (activeLeases.Count > 1)
        {
            throw new InvalidOperationException($"Data integrity violation: Multiple active lease contracts found for tenant {tenantId}.");
        }

        return activeLeases.SingleOrDefault();
    }
}

