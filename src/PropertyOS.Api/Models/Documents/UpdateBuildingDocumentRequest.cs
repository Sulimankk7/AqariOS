using System;

namespace PropertyOS.Api.Models.Documents;

/// <summary>
/// Request model for updating building document metadata.
/// </summary>
public record UpdateBuildingDocumentRequest(
    string DocumentName,
    string? Description = null,
    DateOnly? IssueDate = null,
    DateOnly? ExpiryDate = null,
    bool IsConfidential = false
);
