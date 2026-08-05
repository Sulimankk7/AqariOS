using System;

namespace PropertyOS.Api.Models.Leasing;

public record ReplaceContractDocumentRequest(
    Guid NewFileId,
    string? Description = null
);
