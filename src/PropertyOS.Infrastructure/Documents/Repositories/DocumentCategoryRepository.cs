using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Documents;
using PropertyOS.Domain.Documents.Entities;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Documents.Repositories;

public class DocumentCategoryRepository : IDocumentCategoryRepository
{
    private readonly PropertyOsDbContext _dbContext;

    public DocumentCategoryRepository(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DocumentCategory category, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentCategories.AddAsync(category, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<DocumentCategory> categories, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentCategories.AddRangeAsync(categories, cancellationToken);
    }

    public async Task<DocumentCategory?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DocumentCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId && c.DeletedAt == null, cancellationToken);
    }

    public async Task<List<DocumentCategory>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DocumentCategories
            .Where(c => c.CompanyId == companyId && c.DeletedAt == null)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(Guid companyId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        return await _dbContext.DocumentCategories
            .AnyAsync(c => c.CompanyId == companyId 
                         && c.DeletedAt == null 
                         && (excludeId == null || c.Id != excludeId)
                         && EF.Functions.ILike(c.Name, trimmedName), cancellationToken);
    }

    public async Task<bool> IsCategoryInUseAsync(Guid categoryId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.BuildingDocuments
            .AnyAsync(d => d.CategoryId == categoryId && d.CompanyId == companyId && d.DeletedAt == null, cancellationToken);
    }
}
