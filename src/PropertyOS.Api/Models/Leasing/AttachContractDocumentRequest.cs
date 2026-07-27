using System;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Api.Models.Leasing;

/// <summary>
/// Request model for attaching a confirmed file storage record to a lease contract as a contract document.
/// </summary>
public record AttachContractDocumentRequest(
    Guid FileId,
    ContractDocumentType DocumentType,
    string? Description = null
);
