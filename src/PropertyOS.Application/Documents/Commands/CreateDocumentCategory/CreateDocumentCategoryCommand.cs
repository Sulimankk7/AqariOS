using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Commands.CreateDocumentCategory;

public record CreateDocumentCategoryCommand(
    string Name,
    string? Description
) : ICommand<DocumentCategoryDto>;
