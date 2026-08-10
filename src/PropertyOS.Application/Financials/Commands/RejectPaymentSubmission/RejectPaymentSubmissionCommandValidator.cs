using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.RejectPaymentSubmission;

public class RejectPaymentSubmissionCommandValidator : AbstractValidator<RejectPaymentSubmissionCommand>
{
    public RejectPaymentSubmissionCommandValidator()
    {
        RuleFor(v => v.SubmissionId)
            .NotEmpty().WithMessage("Submission ID is required.");

        RuleFor(v => v.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MinimumLength(5).WithMessage("Rejection reason must be at least 5 characters long.");
    }
}
