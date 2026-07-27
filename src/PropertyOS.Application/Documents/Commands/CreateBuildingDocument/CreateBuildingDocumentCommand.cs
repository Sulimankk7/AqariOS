using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Commands.CreateBuildingDocument;

public record CreateBuildingDocumentCommand(
    Guid BuildingId,
    Guid CategoryId,
    Guid FileId,
    string DocumentName,
    string? Description,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    bool IsConfidential
) : ICommand<BuildingDocumentDto>;
