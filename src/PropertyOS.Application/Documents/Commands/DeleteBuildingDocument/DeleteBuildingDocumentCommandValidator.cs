using FluentValidation;

namespace PropertyOS.Application.Documents.Commands.DeleteBuildingDocument;

public class DeleteBuildingDocumentCommandValidator : AbstractValidator<DeleteBuildingDocumentCommand>
{
    public DeleteBuildingDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
