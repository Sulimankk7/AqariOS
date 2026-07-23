using FluentValidation;

namespace PropertyOS.Application.Documents.Commands.CreateDocumentCategory;

public class CreateDocumentCategoryCommandValidator : AbstractValidator<CreateDocumentCategoryCommand>
{
    public CreateDocumentCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}
