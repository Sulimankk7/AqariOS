using System;

namespace PropertyOS.Application.Documents.DTOs;

public record BuildingDocumentDto(
    Guid Id,
    Guid CompanyId,
    Guid BuildingId,
    Guid CategoryId,
    string CategoryName,
    Guid FileId,
    string OriginalFilename,
    string MimeType,
    long SizeBytes,
    string DocumentName,
    string? Description,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    bool IsConfidential,
    Guid? UploadedBy,
    DateTimeOffset CreatedAt
);
