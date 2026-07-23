using System;

namespace PropertyOS.Application.Documents.DTOs;

public record ExpiringDocumentDto(
    Guid Id,
    Guid BuildingId,
    string BuildingName,
    Guid CategoryId,
    string CategoryName,
    string DocumentName,
    DateOnly ExpiryDate,
    int DaysUntilExpiry,
    bool IsConfidential
);
