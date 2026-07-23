using System;
using MediatR;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Commands.UpdateBuildingDocument;

public record UpdateBuildingDocumentCommand(
    Guid Id,
    string DocumentName,
    string? Description,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    bool IsConfidential
) : IRequest<BuildingDocumentDto>;
