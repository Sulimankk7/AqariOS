using System;

namespace PropertyOS.Api.Models.Documents;

/// <summary>
/// Request model for registering an uploaded file as a building document.
/// </summary>
public record CreateBuildingDocumentRequest(
    Guid CategoryId,
    Guid FileId,
    string DocumentName,
    string? Description = null,
    DateOnly? IssueDate = null,
    DateOnly? ExpiryDate = null,
    bool IsConfidential = false
);
