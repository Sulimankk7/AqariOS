using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Files.DTOs;
using PropertyOS.Application.Files.Services;
using PropertyOS.Domain.Files.Entities;

namespace PropertyOS.Application.Files.Commands.ConfirmFileUpload;

public class ConfirmFileUploadCommandHandler : IRequestHandler<ConfirmFileUploadCommand, FileStorageDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IFileStorageRepository _fileRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly IFileValidationService _validationService;

    public ConfirmFileUploadCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IFileStorageRepository fileRepository,
        IStorageProvider storageProvider,
        IFileValidationService validationService)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _fileRepository = fileRepository;
        _storageProvider = storageProvider;
        _validationService = validationService;
    }

    public async Task<FileStorageDto> Handle(ConfirmFileUploadCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        // Verify storage object exists in storage provider
        var exists = await _storageProvider.ExistsAsync(request.StorageKey, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException($"File object with key '{request.StorageKey}' was not found in storage. Upload must be completed prior to confirmation.");
        }

        // Verify file stream header magic bytes
        using (var readStream = await _storageProvider.GetReadStreamAsync(request.StorageKey, cancellationToken))
        {
            _validationService.ValidateFile(request.OriginalFilename, request.MimeType, request.SizeBytes, readStream);
        }

        var now = DateTimeOffset.UtcNow;
        var sanitizedFilename = _validationService.SanitizeFilename(request.OriginalFilename);

        var fileStorage = FileStorage.Create(
            companyId: companyId,
            uploadedBy: userId,
            originalFilename: sanitizedFilename,
            mimeType: request.MimeType,
            sizeBytes: request.SizeBytes,
            storageKey: request.StorageKey,
            now: now,
            createdBy: userId
        );

        await _fileRepository.AddAsync(fileStorage, cancellationToken);

        return new FileStorageDto(
            Id: fileStorage.Id,
            CompanyId: fileStorage.CompanyId,
            UploadedBy: fileStorage.UploadedBy,
            OriginalFilename: fileStorage.OriginalFilename,
            MimeType: fileStorage.MimeType,
            SizeBytes: fileStorage.SizeBytes,
            StorageKey: fileStorage.StorageKey,
            CreatedAt: fileStorage.CreatedAt
        );
    }
}
