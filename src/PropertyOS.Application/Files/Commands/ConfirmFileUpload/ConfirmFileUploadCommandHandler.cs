using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
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

        // Canonical-form gate: the storage key is client-supplied and the prefix check
        // below is purely textual, so any non-canonical spelling of a path must be
        // rejected BEFORE the prefix/FileId checks. Keys containing traversal segments
        // ("..") or backslashes could otherwise start with the correct "{companyId}/"
        // prefix yet resolve outside the tenant's directory on the storage backend.
        // Canonical keys are forward-slash-separated relative paths.
        var storageKey = request.StorageKey ?? string.Empty;
        if (storageKey.Length == 0
            || storageKey.Contains("..", StringComparison.Ordinal)
            || storageKey.Contains('\\'))
        {
            throw new BusinessRuleException("Storage key is not in canonical form.", "FILE_STORAGE_KEY_INVALID");
        }

        // Tenant containment: the storage key MUST live under the caller's own company
        // prefix ({companyId}/...) — otherwise any tenant could register another
        // tenant's stored object under its own FileStorage row — and it must embed the
        // FileId issued by the upload request ({fileId}-{filename} tail), binding this
        // confirmation to that specific requested upload.
        if (!storageKey.StartsWith($"{companyId:D}/", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("Storage key does not belong to the current tenant.", "FILE_STORAGE_KEY_INVALID");
        }

        if (!storageKey.Contains($"/{request.FileId:D}-", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("Storage key does not match the requested file ID.", "FILE_STORAGE_KEY_INVALID");
        }

        // Verify storage object exists in storage provider
        var exists = await _storageProvider.ExistsAsync(storageKey, cancellationToken);
        if (!exists)
        {
            throw new BusinessRuleException($"File object with key '{storageKey}' was not found in storage. Upload must be completed prior to confirmation.", "FILE_UPLOAD_INCOMPLETE");
        }

        // Client-declared size must match what actually landed on disk (the declared value
        // is what FileValidationService checks against the size limit).
        var actualSizeBytes = await _storageProvider.GetSizeAsync(storageKey, cancellationToken);
        if (actualSizeBytes != request.SizeBytes)
        {
            throw new BusinessRuleException(
                $"Declared size ({request.SizeBytes} bytes) does not match the stored object ({actualSizeBytes} bytes).",
                "FILE_SIZE_MISMATCH");
        }

        // Verify file stream header magic bytes
        using (var readStream = await _storageProvider.GetReadStreamAsync(storageKey, cancellationToken))
        {
            try
            {
                _validationService.ValidateFile(request.OriginalFilename, request.MimeType, request.SizeBytes, readStream);
            }
            catch (ArgumentException ex)
            {
                throw new BusinessRuleException(ex.Message, "FILE_VALIDATION_FAILED");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var sanitizedFilename = _validationService.SanitizeFilename(request.OriginalFilename);

        var fileStorage = FileStorage.Create(
            companyId: companyId,
            uploadedBy: userId,
            originalFilename: sanitizedFilename,
            mimeType: request.MimeType,
            sizeBytes: request.SizeBytes,
            storageKey: storageKey,
            now: now,
            createdBy: userId,
            id: request.FileId // keep the persisted Id identical to the upload-request FileId embedded in the key
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
