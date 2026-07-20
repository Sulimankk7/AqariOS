using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.UpdateCompanyReceiptSequence;

public record UpdateCompanyReceiptSequenceCommand(
    string Prefix,
    short PaddingLength,
    ReceiptResetPolicy ResetPolicy
) : ICommand;
