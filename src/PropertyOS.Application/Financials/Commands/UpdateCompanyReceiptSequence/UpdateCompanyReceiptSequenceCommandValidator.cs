using System;
using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.UpdateCompanyReceiptSequence;

public class UpdateCompanyReceiptSequenceCommandValidator : AbstractValidator<UpdateCompanyReceiptSequenceCommand>
{
    public UpdateCompanyReceiptSequenceCommandValidator()
    {
        RuleFor(x => x.PaddingLength)
            .InclusiveBetween((short)1, (short)10)
            .WithMessage("Padding length must be between 1 and 10.");

        RuleFor(x => x.Prefix)
            .MaximumLength(20)
            .WithMessage("Prefix must not exceed 20 characters.");
    }
}
