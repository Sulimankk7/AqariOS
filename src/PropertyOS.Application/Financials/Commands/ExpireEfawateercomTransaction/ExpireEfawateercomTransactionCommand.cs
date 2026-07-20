using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.ExpireEfawateercomTransaction;

/// <summary>
/// Marks a stale eFAWATEERcom transaction (Pending or Sent) as Timeout.
/// Intended to be dispatched by a background job that sweeps transactions
/// older than the configured gateway response window (e.g. 30 minutes).
/// Idempotent: calling this on an already-terminal transaction is a safe no-op.
/// </summary>
public record ExpireEfawateercomTransactionCommand(
    Guid TransactionId,
    string? ResponseCode = null,
    string? ResponseMessage = null
) : ICommand;
