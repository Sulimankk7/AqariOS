using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Files;
using PropertyOS.Application.Files.Options;
using PropertyOS.Application.Files.Services;

namespace PropertyOS.Application.Documents.Queries.GetDocumentDownloadUrl;

public class GetDocumentDownloadUrlQueryHandler : IRequestHandler<GetDocumentDownloadUrlQuery, DocumentDownloadUrlResponse>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBuildingDocumentRepository _documentRepository;
    private readonly IFileStorageRepository _fileRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly FileStorageOptions _options;

    public GetDocumentDownloadUrlQueryHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IBuildingDocumentRepository documentRepository,
        IFileStorageRepository fileRepository,
        IStorageProvider storageProvider,
        FileStorageOptions options)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _documentRepository = documentRepository;
        _fileRepository = fileRepository;
        _storageProvider = storageProvider;
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<DocumentDownloadUrlResponse> Handle(GetDocumentDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        var canViewConfidential = userId.HasValue && await _documentRepository.CanUserViewConfidentialDocumentsAsync(userId.Value, companyId, cancellationToken);

        var document = await _documentRepository.GetBuildingDocumentDtoByIdAsync(
            id: request.DocumentId,
            companyId: companyId,
            canViewConfidential: canViewConfidential,
            cancellationToken: cancellationToken
        );

        if (document == null)
        {
            throw new KeyNotFoundException($"Building document with ID '{request.DocumentId}' was not found.");
        }

        var fileStorage = await _fileRepository.GetByIdAsync(document.FileId, cancellationToken);
        if (fileStorage == null)
        {
            throw new InvalidOperationException($"Associated file storage record '{document.FileId}' was not found.");
        }

        var downloadUrl = await _storageProvider.GeneratePreSignedDownloadUrlAsync(
            storageKey: fileStorage.StorageKey,
            filename: fileStorage.OriginalFilename,
            expirationMinutes: _options.PreSignedUrlExpirationMinutes,
            cancellationToken: cancellationToken
        );

        return new DocumentDownloadUrlResponse(
            DocumentId: document.Id,
            DownloadUrl: downloadUrl,
            ExpirationMinutes: _options.PreSignedUrlExpirationMinutes
        );
    }
}
