using MediatR;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Commands.CreateDocumentCategory;

public record CreateDocumentCategoryCommand(
    string Name,
    string? Description
) : IRequest<DocumentCategoryDto>;
