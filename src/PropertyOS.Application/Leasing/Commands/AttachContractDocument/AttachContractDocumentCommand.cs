using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Leasing.Commands.AttachContractDocument;

public record AttachContractDocumentCommand(
    Guid LeaseContractId,
    Guid FileId,
    ContractDocumentType DocumentType,
    string? Description = null
) : ICommand<Guid>;
