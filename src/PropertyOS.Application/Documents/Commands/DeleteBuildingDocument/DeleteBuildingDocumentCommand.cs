using System;
using MediatR;

namespace PropertyOS.Application.Documents.Commands.DeleteBuildingDocument;

public record DeleteBuildingDocumentCommand(Guid Id) : IRequest<Unit>;
