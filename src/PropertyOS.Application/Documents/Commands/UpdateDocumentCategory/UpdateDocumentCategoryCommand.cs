using System;
using MediatR;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Commands.UpdateDocumentCategory;

public record UpdateDocumentCategoryCommand(
    Guid Id,
    string Name,
    string? Description
) : IRequest<DocumentCategoryDto>;
