using FluentValidation;

namespace PropertyOS.Application.Documents.Commands.DeleteDocumentCategory;

public class DeleteDocumentCategoryCommandValidator : AbstractValidator<DeleteDocumentCategoryCommand>
{
    public DeleteDocumentCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
