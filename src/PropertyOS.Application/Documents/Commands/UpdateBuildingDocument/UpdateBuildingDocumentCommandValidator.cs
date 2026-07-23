using FluentValidation;

namespace PropertyOS.Application.Documents.Commands.UpdateBuildingDocument;

public class UpdateBuildingDocumentCommandValidator : AbstractValidator<UpdateBuildingDocumentCommand>
{
    public UpdateBuildingDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DocumentName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
