using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Commands.UpdateBuildingDocument;

public record UpdateBuildingDocumentCommand(
    Guid Id,
    string DocumentName,
    string? Description,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    bool IsConfidential
) : ICommand<BuildingDocumentDto>;
