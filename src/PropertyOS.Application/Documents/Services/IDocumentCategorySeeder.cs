using System;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Documents.Services;

public interface IDocumentCategorySeeder
{
    Task SeedDefaultCategoriesAsync(Guid companyId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
