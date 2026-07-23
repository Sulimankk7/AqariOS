using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;
using PropertyOS.Application.Files;
using PropertyOS.Domain.Documents.Entities;

namespace PropertyOS.Application.Documents.Commands.ReplaceBuildingDocument;

public class ReplaceBuildingDocumentCommandHandler : IRequestHandler<ReplaceBuildingDocumentCommand, BuildingDocumentDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBuildingDocumentRepository _documentRepository;
    private readonly IDocumentCategoryRepository _categoryRepository;
    private readonly IFileStorageRepository _fileRepository;

    public ReplaceBuildingDocumentCommandHandler(
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

    public async Task<BuildingDocumentDto> Handle(ReplaceBuildingDocumentCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        // Fetch existing document
        var existingDoc = await _documentRepository.GetByIdAsync(request.ExistingDocumentId, companyId, cancellationToken);
        if (existingDoc == null)
        {
            throw new KeyNotFoundException($"Existing building document with ID '{request.ExistingDocumentId}' was not found.");
        }

        // Verify new file exists in file_storage
        var newFileExists = await _fileRepository.ExistsAsync(request.NewFileId, companyId, cancellationToken);
        if (!newFileExists)
        {
            throw new InvalidOperationException($"New file storage record with ID '{request.NewFileId}' was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);

        var docName = !string.IsNullOrWhiteSpace(request.NewDocumentName) ? request.NewDocumentName : existingDoc.DocumentName;
        var description = request.NewDescription ?? existingDoc.Description;
        var issueDate = request.NewIssueDate ?? existingDoc.IssueDate;
        var expiryDate = request.NewExpiryDate ?? existingDoc.ExpiryDate;
        var isConfidential = request.NewIsConfidential ?? existingDoc.IsConfidential;

        // 1. Create new replacement BuildingDocument record
        var newDocument = BuildingDocument.Create(
            companyId: companyId,
            buildingId: existingDoc.BuildingId,
            categoryId: existingDoc.CategoryId,
            fileId: request.NewFileId,
            documentName: docName,
            description: description,
            issueDate: issueDate,
            expiryDate: expiryDate,
            isConfidential: isConfidential,
            uploadedBy: userId,
            now: now,
            createdBy: userId,
            currentDate: today
        );

        // 2. Soft-delete old document record (retaining historical trail)
        existingDoc.SoftDelete(now, userId);

        // 3. Persist new document
        await _documentRepository.AddAsync(newDocument, cancellationToken);

        var category = await _categoryRepository.GetByIdAsync(newDocument.CategoryId, companyId, cancellationToken);
        var fileStorage = await _fileRepository.GetByIdAsync(newDocument.FileId, cancellationToken);

        return new BuildingDocumentDto(
            Id: newDocument.Id,
            CompanyId: newDocument.CompanyId,
            BuildingId: newDocument.BuildingId,
            CategoryId: newDocument.CategoryId,
            CategoryName: category?.Name ?? "",
            FileId: newDocument.FileId,
            OriginalFilename: fileStorage?.OriginalFilename ?? "",
            MimeType: fileStorage?.MimeType ?? "",
            SizeBytes: fileStorage?.SizeBytes ?? 0,
            DocumentName: newDocument.DocumentName,
            Description: newDocument.Description,
            IssueDate: newDocument.IssueDate,
            ExpiryDate: newDocument.ExpiryDate,
            IsConfidential: newDocument.IsConfidential,
            UploadedBy: newDocument.UploadedBy,
            CreatedAt: newDocument.CreatedAt
        );
    }
}
