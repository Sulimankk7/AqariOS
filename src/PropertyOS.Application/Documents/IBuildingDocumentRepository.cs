using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Documents.DTOs;
using PropertyOS.Application.Documents.Queries.GetBuildingDocuments;
using PropertyOS.Domain.Documents.Entities;

namespace PropertyOS.Application.Documents;

public interface IBuildingDocumentRepository
{
    Task AddAsync(BuildingDocument document, CancellationToken cancellationToken = default);
    Task<BuildingDocument?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByBuildingAndFileAsync(Guid buildingId, Guid fileId, CancellationToken cancellationToken = default);

    Task<PagedBuildingDocumentsResponse> GetBuildingDocumentsAsync(
        Guid companyId,
        Guid? buildingId,
        Guid? categoryId,
        string? searchTerm,
        bool canViewConfidential,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<BuildingDocumentDto?> GetBuildingDocumentDtoByIdAsync(
        Guid id,
        Guid companyId,
        bool canViewConfidential,
        CancellationToken cancellationToken = default);

    Task<List<ExpiringDocumentDto>> GetExpiringDocumentsAsync(
        Guid companyId,
        int withinDays,
        bool canViewConfidential,
        CancellationToken cancellationToken = default);

    Task<bool> CanUserViewConfidentialDocumentsAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default);
}
