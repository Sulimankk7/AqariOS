using System;
using MediatR;

namespace PropertyOS.Application.Documents.Commands.DeleteDocumentCategory;

public record DeleteDocumentCategoryCommand(Guid Id) : IRequest<Unit>;
