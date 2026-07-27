using System;

namespace PropertyOS.Api.Models.Documents;

/// <summary>
/// Request model for replacing a building document's file with a new upload.
/// Null metadata fields fall back to the existing document's values.
/// </summary>
public record ReplaceBuildingDocumentRequest(
    Guid NewFileId,
    string? NewDocumentName = null,
    string? NewDescription = null,
    DateOnly? NewIssueDate = null,
    DateOnly? NewExpiryDate = null,
    bool? NewIsConfidential = null
);
