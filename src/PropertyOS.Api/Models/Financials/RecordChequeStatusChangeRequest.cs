using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Request model for recording a cheque lifecycle status change.
/// </summary>
public record RecordChequeStatusChangeRequest(
    ChequeStatus NewStatus,
    DateOnly ActionDate,
    string? BounceReason = null,
    decimal? BounceFeeCharged = null,
    string? CancellationReason = null,
    Guid? ReplacementChequeId = null,
    string? Notes = null
);
