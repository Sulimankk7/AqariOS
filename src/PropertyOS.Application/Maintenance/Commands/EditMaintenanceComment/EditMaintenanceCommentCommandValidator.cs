using FluentValidation;

namespace PropertyOS.Application.Maintenance.Commands.EditMaintenanceComment;

public class EditMaintenanceCommentCommandValidator : AbstractValidator<EditMaintenanceCommentCommand>
{
    public EditMaintenanceCommentCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("Request ID must be specified.");

        RuleFor(x => x.CommentId)
            .NotEmpty()
            .WithMessage("Comment ID must be specified.");

        RuleFor(x => x.NewText)
            .NotEmpty()
            .WithMessage("Comment text must not be blank.")
            // Column is unbounded text; 4000 is an application-level sanity bound.
            .MaximumLength(4000)
            .WithMessage("Comment text must not exceed 4000 characters.");
    }
}
