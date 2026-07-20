using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.ReceiveEfawateercomCallback;

public record ReceiveEfawateercomCallbackCommand(
    string ExternalTransactionId,
    EfawateercomStatus Status,
    DateTimeOffset ResponseTime,
    string? ResponseCode = null,
    string? ResponseMessage = null,
    string? RawResponse = null
) : ICommand;
