using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.ReplaceContractDocument;

public record ReplaceContractDocumentCommand(
    Guid LeaseContractId,
    Guid DocumentId,
    Guid NewFileId,
    string? Description = null
) : ICommand<Unit>;
