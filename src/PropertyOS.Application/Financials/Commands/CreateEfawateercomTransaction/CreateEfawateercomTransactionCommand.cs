using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.CreateEfawateercomTransaction;

public record CreateEfawateercomTransactionCommand(
    Guid RentPaymentId,
    string ExternalTransactionId,
    decimal Amount,
    string? PaymentReference = null
) : ICommand;
