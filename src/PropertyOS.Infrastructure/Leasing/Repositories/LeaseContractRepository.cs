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

    public async Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default)
    {
        await _dbContext.ContractStatusHistory.AddAsync(statusHistory, cancellationToken);
    }

    public async Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default)
    {
        await _dbContext.ContractTerminations.AddAsync(termination, cancellationToken);
    }

    public async Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contractDto = await _dbContext.LeaseContracts
            .AsNoTracking()
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

        contractDto.Documents = await _dbContext.ContractDocuments
            .AsNoTracking()
            .Where(d => d.LeaseContractId == id)
            .OrderByDescending(d => d.CreatedAt)
            .ProjectToType<ContractDocumentDto>()
            .ToListAsync(cancellationToken);

        contractDto.Termination = await _dbContext.ContractTerminations
            .AsNoTracking()
            .Where(t => t.LeaseContractId == id)
            .ProjectToType<ContractTerminationDto>()
            .FirstOrDefaultAsync(cancellationToken);

        return contractDto;
    }

    public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.LeaseContracts
            .AsNoTracking()
            .Where(c => c.ApartmentId == apartmentId)
            .OrderByDescending(c => c.StartDate)
            .ProjectToType<LeaseContractDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return _dbContext.LeaseContracts
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.StartDate)
            .ProjectToType<LeaseContractDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return _dbContext.LeaseContracts
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .ProjectToType<LeaseContractDto>()
                .ToListAsync(cancellationToken);
        }

        var normalizedSearch = searchTerm.Trim().ToLower();

        return _dbContext.LeaseContracts
            .AsNoTracking()
            .Where(c => EF.Functions.ILike(c.ContractNumber, $"%{normalizedSearch}%") ||
                        (c.ExternalRegistrationRef != null && EF.Functions.ILike(c.ExternalRegistrationRef, $"%{normalizedSearch}%")))
            .OrderByDescending(c => c.CreatedAt)
            .ProjectToType<LeaseContractDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var limitDate = today.AddDays(daysAhead);

        return _dbContext.LeaseContracts
            .AsNoTracking()
            .Where(c => c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Active
                        && c.EndDate >= today
                        && c.EndDate <= limitDate)
            .OrderBy(c => c.EndDate)
            .ProjectToType<LeaseContractDto>()
            .ToListAsync(cancellationToken);
    }
}
