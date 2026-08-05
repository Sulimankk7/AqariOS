using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.DeleteContractDocument;

public record DeleteContractDocumentCommand(
    Guid LeaseContractId,
    Guid DocumentId
) : ICommand<Unit>;
