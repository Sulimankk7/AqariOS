using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Commands.UpdateDocumentCategory;

public record UpdateDocumentCategoryCommand(
    Guid Id,
    string Name,
    string? Description
) : ICommand<DocumentCategoryDto>;
