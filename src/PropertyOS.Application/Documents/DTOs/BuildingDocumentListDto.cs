using System;

namespace PropertyOS.Application.Documents.DTOs;

public record BuildingDocumentListDto(
    Guid Id,
    Guid BuildingId,
    Guid CategoryId,
    string CategoryName,
    Guid FileId,
    string DocumentName,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    bool IsConfidential,
    DateTimeOffset CreatedAt
);
