using FluentValidation;

namespace PropertyOS.Application.Documents.Commands.ReplaceBuildingDocument;

public class ReplaceBuildingDocumentCommandValidator : AbstractValidator<ReplaceBuildingDocumentCommand>
{
    public ReplaceBuildingDocumentCommandValidator()
    {
        RuleFor(x => x.ExistingDocumentId).NotEmpty();
        RuleFor(x => x.NewFileId).NotEmpty();
        RuleFor(x => x.NewDocumentName).MaximumLength(255);
        RuleFor(x => x.NewDescription).MaximumLength(2000);
    }
}
