using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Files.Entities;

namespace PropertyOS.Application.Files;

public interface IFileStorageRepository
{
    Task AddAsync(FileStorage fileStorage, CancellationToken cancellationToken = default);
    Task<FileStorage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
}
