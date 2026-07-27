using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Documents.Commands.DeleteDocumentCategory;

public record DeleteDocumentCategoryCommand(Guid Id) : ICommand;
