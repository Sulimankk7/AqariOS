using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Commands.ReplaceBuildingDocument;

public record ReplaceBuildingDocumentCommand(
    Guid ExistingDocumentId,
    Guid NewFileId,
    string? NewDocumentName,
    string? NewDescription,
    DateOnly? NewIssueDate,
    DateOnly? NewExpiryDate,
    bool? NewIsConfidential
) : ICommand<BuildingDocumentDto>;
