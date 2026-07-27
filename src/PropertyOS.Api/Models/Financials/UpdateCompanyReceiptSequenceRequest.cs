using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Request model for updating the current company's official receipt numbering sequence.
/// </summary>
public record UpdateCompanyReceiptSequenceRequest(
    string Prefix,
    short PaddingLength,
    ReceiptResetPolicy ResetPolicy
);
