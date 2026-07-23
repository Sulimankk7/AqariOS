using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;
using PropertyOS.Application.Files;
using PropertyOS.Domain.Documents.Entities;

namespace PropertyOS.Application.Documents.Commands.CreateBuildingDocument;

public class CreateBuildingDocumentCommandHandler : IRequestHandler<CreateBuildingDocumentCommand, BuildingDocumentDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBuildingDocumentRepository _documentRepository;
    private readonly IDocumentCategoryRepository _categoryRepository;
    private readonly IFileStorageRepository _fileRepository;

    public CreateBuildingDocumentCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IBuildingDocumentRepository documentRepository,
        IDocumentCategoryRepository categoryRepository,
        IFileStorageRepository fileRepository)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _documentRepository = documentRepository;
        _categoryRepository = categoryRepository;
        _fileRepository = fileRepository;
    }

    public async Task<BuildingDocumentDto> Handle(CreateBuildingDocumentCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        // Verify category exists in company
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, companyId, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Document category with ID '{request.CategoryId}' was not found.");
        }

        // Verify file exists in file_storage
        var fileExists = await _fileRepository.ExistsAsync(request.FileId, companyId, cancellationToken);
        if (!fileExists)
        {
            throw new InvalidOperationException($"File storage record with ID '{request.FileId}' was not found in file_storage.");
        }

        // Verify file is not attached twice to the same building
        var alreadyAttached = await _documentRepository.ExistsByBuildingAndFileAsync(request.BuildingId, request.FileId, cancellationToken);
        if (alreadyAttached)
        {
            throw new InvalidOperationException($"File '{request.FileId}' is already attached to building '{request.BuildingId}'.");
        }

        var fileStorage = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);

        var document = BuildingDocument.Create(
            companyId: companyId,
            buildingId: request.BuildingId,
            categoryId: request.CategoryId,
            fileId: request.FileId,
            documentName: request.DocumentName,
            description: request.Description,
            issueDate: request.IssueDate,
            expiryDate: request.ExpiryDate,
            isConfidential: request.IsConfidential,
            uploadedBy: userId,
            now: now,
            createdBy: userId,
            currentDate: today
        );

        await _documentRepository.AddAsync(document, cancellationToken);

        return new BuildingDocumentDto(
            Id: document.Id,
            CompanyId: document.CompanyId,
            BuildingId: document.BuildingId,
            CategoryId: document.CategoryId,
            CategoryName: category.Name,
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
}
