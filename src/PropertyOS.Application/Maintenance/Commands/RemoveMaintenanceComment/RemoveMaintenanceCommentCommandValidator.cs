using FluentValidation;

namespace PropertyOS.Application.Maintenance.Commands.RemoveMaintenanceComment;

public class RemoveMaintenanceCommentCommandValidator : AbstractValidator<RemoveMaintenanceCommentCommand>
{
    public RemoveMaintenanceCommentCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("Request ID must be specified.");

        RuleFor(x => x.CommentId)
            .NotEmpty()
            .WithMessage("Comment ID must be specified.");
    }
}
