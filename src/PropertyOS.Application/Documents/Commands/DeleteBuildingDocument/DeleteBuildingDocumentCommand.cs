using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Documents.Commands.DeleteBuildingDocument;

public record DeleteBuildingDocumentCommand(Guid Id) : ICommand;
