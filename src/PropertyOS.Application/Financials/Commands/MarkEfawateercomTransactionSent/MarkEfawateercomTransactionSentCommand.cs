using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.MarkEfawateercomTransactionSent;

/// <summary>
/// Transitions an eFAWATEERcom transaction from Pending to Sent.
/// Called by the API controller immediately after a successful outbound dispatch
/// to the eFAWATEERcom payment gateway, before awaiting the callback.
/// </summary>
public record MarkEfawateercomTransactionSentCommand(Guid TransactionId) : ICommand;
