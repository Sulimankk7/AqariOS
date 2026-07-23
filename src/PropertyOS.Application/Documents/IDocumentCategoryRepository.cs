using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Documents.Entities;

namespace PropertyOS.Application.Documents;

public interface IDocumentCategoryRepository
{
    Task AddAsync(DocumentCategory category, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<DocumentCategory> categories, CancellationToken cancellationToken = default);
    Task<DocumentCategory?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<List<DocumentCategory>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(Guid companyId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsCategoryInUseAsync(Guid categoryId, Guid companyId, CancellationToken cancellationToken = default);
}
