using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Files;
using PropertyOS.Domain.Files.Entities;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Files.Repositories;

public class FileStorageRepository : IFileStorageRepository
{
    private readonly PropertyOsDbContext _dbContext;

    public FileStorageRepository(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(FileStorage fileStorage, CancellationToken cancellationToken = default)
    {
        await _dbContext.FileStorage.AddAsync(fileStorage, cancellationToken);
    }

    public async Task<FileStorage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FileStorage
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FileStorage
            .AnyAsync(f => f.Id == id && f.CompanyId == companyId && f.DeletedAt == null, cancellationToken);
    }
}
