using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Marketplace.Services;

/// <summary>
/// Production FileStorage validator checking central file storage records in database.
/// Implements IFileStorageValidator to validate file presence, company scope, and soft-delete status across modules.
/// </summary>
public class FileStorageValidator : IFileStorageValidator
{
    private readonly PropertyOsDbContext _dbContext;

    public FileStorageValidator(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ValidateImageFileAsync(Guid fileId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FileStorage
            .AsNoTracking()
            .AnyAsync(
                f => f.Id == fileId 
                     && f.CompanyId == companyId 
                     && f.DeletedAt == null, 
                cancellationToken);
    }
}
