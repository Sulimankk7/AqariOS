using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.RecordChequeStatusChange;

public record RecordChequeStatusChangeCommand(
    Guid ChequeId,
    ChequeStatus NewStatus,
    DateOnly ActionDate,
    string? BounceReason = null,
    decimal? BounceFeeCharged = null,
    string? CancellationReason = null,
    Guid? ReplacementChequeId = null,
    string? Notes = null) : ICommand;
