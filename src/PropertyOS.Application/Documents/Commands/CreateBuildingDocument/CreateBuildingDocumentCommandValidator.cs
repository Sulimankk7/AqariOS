using FluentValidation;

namespace PropertyOS.Application.Documents.Commands.CreateBuildingDocument;

public class CreateBuildingDocumentCommandValidator : AbstractValidator<CreateBuildingDocumentCommand>
{
    public CreateBuildingDocumentCommandValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.FileId).NotEmpty();
        RuleFor(x => x.DocumentName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
