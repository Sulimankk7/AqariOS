using FluentValidation;

namespace PropertyOS.Application.Maintenance.Commands.AddMaintenanceComment;

public class AddMaintenanceCommentCommandValidator : AbstractValidator<AddMaintenanceCommentCommand>
{
    public AddMaintenanceCommentCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("Request ID must be specified.");

        RuleFor(x => x.CommentText)
            .NotEmpty()
            .WithMessage("Comment text must not be blank.")
            // Column is unbounded text; 4000 is an application-level sanity bound.
            .MaximumLength(4000)
            .WithMessage("Comment text must not exceed 4000 characters.");
    }
}
