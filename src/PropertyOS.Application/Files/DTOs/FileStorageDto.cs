using System;

namespace PropertyOS.Application.Files.DTOs;

public record FileStorageDto(
    Guid Id,
    Guid CompanyId,
    Guid? UploadedBy,
    string OriginalFilename,
    string MimeType,
    long SizeBytes,
    string StorageKey,
    DateTimeOffset CreatedAt
);
