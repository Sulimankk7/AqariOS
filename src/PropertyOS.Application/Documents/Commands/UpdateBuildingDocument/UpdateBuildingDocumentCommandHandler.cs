using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;
using PropertyOS.Application.Files;

namespace PropertyOS.Application.Documents.Commands.UpdateBuildingDocument;

public class UpdateBuildingDocumentCommandHandler : IRequestHandler<UpdateBuildingDocumentCommand, BuildingDocumentDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBuildingDocumentRepository _documentRepository;
    private readonly IDocumentCategoryRepository _categoryRepository;
    private readonly IFileStorageRepository _fileRepository;

    public UpdateBuildingDocumentCommandHandler(
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

    public async Task<BuildingDocumentDto> Handle(UpdateBuildingDocumentCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        var document = await _documentRepository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (document == null)
        {
            throw new NotFoundException($"Building document with ID '{request.Id}' was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);

        try
        {
            document.UpdateDetails(
                newName: request.DocumentName,
                newDescription: request.Description,
                newIssueDate: request.IssueDate,
                newExpiryDate: request.ExpiryDate,
                newIsConfidential: request.IsConfidential,
                now: now,
                updatedBy: userId,
                currentDate: today
            );
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message, "DOCUMENT_INVALID_STATE");
        }

        var category = await _categoryRepository.GetByIdAsync(document.CategoryId, companyId, cancellationToken);
        var fileStorage = await _fileRepository.GetByIdAsync(document.FileId, cancellationToken);

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
}
