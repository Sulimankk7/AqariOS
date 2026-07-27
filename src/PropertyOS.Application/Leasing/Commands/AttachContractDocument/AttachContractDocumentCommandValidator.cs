using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.AttachContractDocument;

public class AttachContractDocumentCommandValidator : AbstractValidator<AttachContractDocumentCommand>
{
    public AttachContractDocumentCommandValidator()
    {
        RuleFor(x => x.LeaseContractId).NotEmpty();
        RuleFor(x => x.FileId).NotEmpty();
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(255);
    }
}
