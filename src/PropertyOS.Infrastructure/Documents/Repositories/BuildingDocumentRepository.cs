using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Documents;
using PropertyOS.Application.Documents.DTOs;
using PropertyOS.Application.Documents.Queries.GetBuildingDocuments;
using PropertyOS.Domain.Documents.Entities;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Documents.Repositories;

public class BuildingDocumentRepository : IBuildingDocumentRepository
{
    private readonly PropertyOsDbContext _dbContext;

    public BuildingDocumentRepository(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(BuildingDocument document, CancellationToken cancellationToken = default)
    {
        await _dbContext.BuildingDocuments.AddAsync(document, cancellationToken);
    }

    public async Task<BuildingDocument?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.BuildingDocuments
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId && d.DeletedAt == null, cancellationToken);
    }

    public async Task<bool> ExistsByBuildingAndFileAsync(Guid buildingId, Guid fileId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.BuildingDocuments
            .AnyAsync(d => d.BuildingId == buildingId && d.FileId == fileId && d.DeletedAt == null, cancellationToken);
    }

    public async Task<PagedBuildingDocumentsResponse> GetBuildingDocumentsAsync(
        Guid companyId,
        Guid? buildingId,
        Guid? categoryId,
        string? searchTerm,
        bool canViewConfidential,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BuildingDocuments
            .AsNoTracking()
            .Where(d => d.CompanyId == companyId && d.DeletedAt == null);

        if (buildingId.HasValue)
        {
            query = query.Where(d => d.BuildingId == buildingId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(d => d.CategoryId == categoryId.Value);
        }

        if (!canViewConfidential)
        {
            query = query.Where(d => !d.IsConfidential);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim();
            query = query.Where(d => EF.Functions.ILike(d.DocumentName, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pNum = pageNumber > 0 ? pageNumber : 1;
        var pSize = pageSize > 0 && pageSize <= 200 ? pageSize : 50;

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((pNum - 1) * pSize)
            .Take(pSize)
            .Join(
                _dbContext.DocumentCategories.AsNoTracking(),
                doc => doc.CategoryId,
                cat => cat.Id,
                (doc, cat) => new BuildingDocumentListDto(
                    doc.Id,
                    doc.BuildingId,
                    doc.CategoryId,
                    cat.Name,
                    doc.FileId,
                    doc.DocumentName,
                    doc.IssueDate,
                    doc.ExpiryDate,
                    doc.IsConfidential,
                    doc.CreatedAt
                )
            )
            .ToListAsync(cancellationToken);

        var hasNextPage = (pNum * pSize) < totalCount;

        return new PagedBuildingDocumentsResponse(
            Items: items,
            TotalCount: totalCount,
            PageNumber: pNum,
            PageSize: pSize,
            HasNextPage: hasNextPage
        );
    }

    public async Task<BuildingDocumentDto?> GetBuildingDocumentDtoByIdAsync(
        Guid id,
        Guid companyId,
        bool canViewConfidential,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.BuildingDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId && d.DeletedAt == null, cancellationToken);

        if (document == null)
            return null;

        if (document.IsConfidential && !canViewConfidential)
        {
            throw new UnauthorizedAccessException("Access denied. Viewing confidential building documents requires 'documents.view_confidential' permission.");
        }

        var category = await _dbContext.DocumentCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == document.CategoryId, cancellationToken);

        var fileStorage = await _dbContext.FileStorage.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == document.FileId, cancellationToken);

        return new BuildingDocumentDto(
            Id: document.Id,
            CompanyId: document.CompanyId,
            BuildingId: document.BuildingId,
            CategoryId: document.CategoryId,
            CategoryName: category?.Name ?? "",
            FileId: document.FileId,
            OriginalFilename: fileStorage?.OriginalFilename ?? "",
            MimeType: fileStorage?.MimeType ?? "",
            SizeBytes: fileStorage?.SizeBytes ?? 0,
            DocumentName: document.DocumentName,
            Description: document.Description,
            IssueDate: document.IssueDate,
            ExpiryDate: document.ExpiryDate,
            IsConfidential: document.IsConfidential,
            UploadedBy: document.UploadedBy,
            CreatedAt: document.CreatedAt
        );
    }

    public async Task<List<ExpiringDocumentDto>> GetExpiringDocumentsAsync(
        Guid companyId,
        int withinDays,
        bool canViewConfidential,
        CancellationToken cancellationToken = default)
    {
        var days = withinDays > 0 ? withinDays : 30;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = today.AddDays(days);

        var query = _dbContext.BuildingDocuments
            .AsNoTracking()
            .Where(d => d.CompanyId == companyId 
                     && d.DeletedAt == null 
                     && d.ExpiryDate != null 
                     && d.ExpiryDate >= today 
                     && d.ExpiryDate <= endDate);

        if (!canViewConfidential)
        {
            query = query.Where(d => !d.IsConfidential);
        }

        var results = await (from doc in query
                             join b in _dbContext.Buildings.AsNoTracking() on doc.BuildingId equals b.Id
                             join cat in _dbContext.DocumentCategories.AsNoTracking() on doc.CategoryId equals cat.Id
                             orderby doc.ExpiryDate ascending
                             select new
                             {
                                 doc.Id,
                                 doc.BuildingId,
                                 BuildingName = b.Name,
                                 doc.CategoryId,
                                 CategoryName = cat.Name,
                                 doc.DocumentName,
                                 ExpiryDate = doc.ExpiryDate!.Value,
                                 doc.IsConfidential
                             }).ToListAsync(cancellationToken);

        return results.Select(r => new ExpiringDocumentDto(
            Id: r.Id,
            BuildingId: r.BuildingId,
            BuildingName: r.BuildingName,
            CategoryId: r.CategoryId,
            CategoryName: r.CategoryName,
            DocumentName: r.DocumentName,
            ExpiryDate: r.ExpiryDate,
            DaysUntilExpiry: r.ExpiryDate.DayNumber - today.DayNumber,
            IsConfidential: r.IsConfidential
        )).ToList();
    }

    public async Task<bool> CanUserViewConfidentialDocumentsAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await (from ucr in _dbContext.UserCompanyRoles.AsNoTracking()
                      join rp in _dbContext.RolePermissions.AsNoTracking() on ucr.RoleId equals rp.RoleId
                      join p in _dbContext.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                      where ucr.UserId == userId && ucr.CompanyId == companyId && ucr.DeletedAt == null
                            && p.Key == "documents.view_confidential"
                      select p.Id).AnyAsync(cancellationToken);
    }
}
